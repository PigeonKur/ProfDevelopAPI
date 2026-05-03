using Microsoft.EntityFrameworkCore;
using ProfDevelopAPI.Models;
using ProfDevelopAPI.Models.DTOs;
using ProfDevelopAPI.Services.Interfaces;

namespace ProfDevelopAPI.Services.Implementations;

public class ProgressService : IProgressService
{
    private readonly PostgresContext _db;
    public ProgressService(PostgresContext db) => _db = db;

    public async Task<SubmitProgressResponse> SubmitAsync(int userId, SubmitProgressRequest request)
    {
        var lesson = await _db.Lessons.FindAsync(request.LessonId)
            ?? throw new KeyNotFoundException("Урок не найден");

        var passed = request.MaxScore > 0
                    && (request.Score * 100 / request.MaxScore) >= 70;
        var xpEarned = passed ? lesson.XpReward : 0;

        // 2x XP boost: если у пользователя активен буст, удваиваем награду.
        var preBoostStats = await _db.UserStats.AsNoTracking().FirstOrDefaultAsync(s => s.UserId == userId);
        var boostActive = preBoostStats?.BoostActiveUntil is { } until
                          && DateTime.SpecifyKind(until, DateTimeKind.Utc) > DateTime.UtcNow;
        if (boostActive && xpEarned > 0)
        {
            xpEarned *= 2;
        }

        var progress = await _db.LessonProgresses
            .FirstOrDefaultAsync(p => p.UserId == userId && p.LessonId == request.LessonId);

        if (progress == null)
        {
            progress = new LessonProgress
            {
                UserId = userId,
                LessonId = request.LessonId,
                Attempts = 1
            };
            _db.LessonProgresses.Add(progress);
        }
        else
        {
            progress.Attempts++;
        }

        if (!progress.IsCompleted || request.Score > progress.Score)
        {
            progress.Score = request.Score;
            progress.MaxScore = request.MaxScore;
            progress.XpEarned = xpEarned;
            progress.IsCompleted = passed;
            if (passed) progress.CompletedAt = DateTime.UtcNow;
            progress.UpdatedAt = DateTime.UtcNow;
        }

        var stats = await _db.UserStats.FindAsync(userId);
        if (stats == null)
        {
            stats = new UserStat { UserId = userId };
            _db.UserStats.Add(stats);
        }

        if (passed)
        {
            stats.TotalXp += xpEarned;
            stats.Level = CalculateLevel(stats.TotalXp);
        }

        var today = GetLocalToday();
        if (stats.LastActiveDate == null || stats.LastActiveDate < today)
        {
            var yesterday = today.AddDays(-1);
            stats.StreakDays = (stats.LastActiveDate == yesterday)
                ? stats.StreakDays + 1
                : 1;

            if (stats.StreakDays > stats.MaxStreak)
                stats.MaxStreak = stats.StreakDays;

            stats.LastActiveDate = today;
        }
        stats.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        var newAchievements = await CheckAchievementsAsync(userId, stats);

        return new SubmitProgressResponse(
            passed,
            xpEarned,
            stats.TotalXp,
            stats.Level,
            stats.StreakDays,
            newAchievements
        );
    }

    public Task<QuestionCheckResultDto> CheckQuestionAsync(QuestionCheckRequest request)
        => CheckQuestionAsync(0, request);

    public async Task<QuestionCheckResultDto> CheckQuestionAsync(int userId, QuestionCheckRequest request)
    {
        var question = await _db.Questions
            .Include(q => q.Answers)
            .Include(q => q.MatchingPairs)
            .FirstOrDefaultAsync(q => q.Id == request.QuestionId)
            ?? throw new KeyNotFoundException("Вопрос не найден");

        var result = EvaluateQuestion(
            question,
            new QuestionAttemptDto(
                request.QuestionId,
                request.SelectedAnswerIds,
                request.MatchingPairs
            )
        );

        if (userId > 0)
        {
            var attempt = await _db.QuestionAttempts
                .FirstOrDefaultAsync(qa => qa.UserId == userId && qa.QuestionId == request.QuestionId);
            var nowUtc = DateTime.UtcNow;
            var isPractice = string.Equals(request.Mode, "practice", StringComparison.OrdinalIgnoreCase);
            if (attempt == null)
            {
                _db.QuestionAttempts.Add(new QuestionAttempt
                {
                    UserId = userId,
                    QuestionId = request.QuestionId,
                    IsCorrect = result.IsCorrect,
                    LastAttemptAt = nowUtc
                });
            }
            else
            {
                // В режиме урока правильный повторный ответ НЕ "графует"
                // вопрос. Иначе очередь "вернуть в конец" сама же лечит ошибку:
                // вопрос автоматически становится IsCorrect=true и больше не
                // попадает в практику. Поднять false→true может только практика.
                var isUpgrade = !attempt.IsCorrect && result.IsCorrect;
                if (isPractice || !isUpgrade)
                {
                    attempt.IsCorrect = result.IsCorrect;
                }
                attempt.LastAttemptAt = nowUtc;
            }
            await _db.SaveChangesAsync();
        }

        return result;
    }

    public async Task<LessonAttemptResultDto> SubmitLessonAttemptAsync(int userId, LessonAttemptRequest request)
    {
        var lesson = await _db.Lessons
            .Include(l => l.Questions)
                .ThenInclude(q => q.Answers)
            .Include(l => l.Questions)
                .ThenInclude(q => q.MatchingPairs)
            .FirstOrDefaultAsync(l => l.Id == request.LessonId)
            ?? throw new KeyNotFoundException("Урок не найден");

        var review = new List<QuestionReviewDto>();
        var score = 0;
        var maxScore = lesson.Questions.Count;

        var questionIds = lesson.Questions.Select(q => q.Id).ToList();
        var existingAttempts = await _db.QuestionAttempts
            .Where(qa => qa.UserId == userId && questionIds.Contains(qa.QuestionId))
            .ToDictionaryAsync(qa => qa.QuestionId);

        var nowUtc = DateTime.UtcNow;
        foreach (var question in lesson.Questions.OrderBy(q => q.OrderIndex))
        {
            var answer = request.Answers.FirstOrDefault(x => x.QuestionId == question.Id)
                ?? new QuestionAttemptDto(question.Id, null, null);

            var evaluation = EvaluateQuestion(question, answer);
            if (evaluation.IsCorrect)
                score++;

            if (existingAttempts.TryGetValue(question.Id, out var attempt))
            {
                // Финальный submit урока никогда не апгрейдит false→true
                // (см. CheckQuestionAsync — ту же логику, чтобы очередь
                // "вернуть в конец" не графовала ошибочные вопросы).
                var isUpgrade = !attempt.IsCorrect && evaluation.IsCorrect;
                if (!isUpgrade)
                {
                    attempt.IsCorrect = evaluation.IsCorrect;
                }
                attempt.LastAttemptAt = nowUtc;
            }
            else
            {
                _db.QuestionAttempts.Add(new QuestionAttempt
                {
                    UserId = userId,
                    QuestionId = question.Id,
                    IsCorrect = evaluation.IsCorrect,
                    LastAttemptAt = nowUtc
                });
            }

            review.Add(new QuestionReviewDto(
                evaluation.QuestionId,
                evaluation.IsCorrect,
                evaluation.Explanation,
                evaluation.CorrectAnswerIds,
                evaluation.CorrectMatchingPairs
            ));
        }
        await _db.SaveChangesAsync();

        var submitResult = await SubmitAsync(userId, new SubmitProgressRequest(
            request.LessonId,
            score,
            maxScore
        ));

        return new LessonAttemptResultDto(
            submitResult.IsCompleted,
            score,
            maxScore,
            submitResult.XpEarned,
            submitResult.TotalXp,
            submitResult.NewLevel,
            submitResult.StreakDays,
            submitResult.NewAchievements,
            review
        );
    }

    public async Task<List<CourseDto>> GetUserCoursesAsync(int userId)
    {
        var progresses = await _db.VCourseProgresses
            .Where(p => p.UserId == userId)
            .ToListAsync();

        return progresses.Select(p => new CourseDto(
            p.CourseId ?? 0,
            0,
            p.CourseTitle ?? "",
            null,
            p.Category,
            null,
            null,
            null,
            null,
            0,
            true,
            (int)(p.TotalLessons ?? 0),
            (int)(p.CompletedLessons ?? 0),
            (int)(p.ProgressPct ?? 0),
            p.IsMandatory,
            p.Deadline
        )).ToList();
    }

    public async Task<bool> AssignCourseAsync(AssignCourseRequest request, int assignedBy)
    {
        var exists = await _db.CourseAssignments
            .AnyAsync(a => a.UserId == request.UserId && a.CourseId == request.CourseId);

        if (exists) return false;

        _db.CourseAssignments.Add(new CourseAssignment
        {
            UserId = request.UserId,
            CourseId = request.CourseId,
            AssignedBy = assignedBy,
            IsMandatory = request.IsMandatory,
            Deadline = request.Deadline,
            AssignedAt = DateTime.UtcNow
        });

        if (!await _db.UserStats.AnyAsync(s => s.UserId == request.UserId))
            _db.UserStats.Add(new UserStat { UserId = request.UserId });

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<XpBoostStatusDto> ActivateXpBoostAsync(int userId, int durationMinutes, int? dailyXpGoal = null)
    {
        var minutes = durationMinutes <= 0 ? 30 : Math.Min(durationMinutes, 240);
        var stats = await _db.UserStats.FirstOrDefaultAsync(s => s.UserId == userId);
        if (stats == null)
        {
            stats = new UserStat { UserId = userId };
            _db.UserStats.Add(stats);
        }
        var now = DateTime.UtcNow;
        var (lessonsToday, xpToday) = await GetTodayProgressAsync(userId);
        var eligible = lessonsToday >= BoostMinLessons || xpToday >= ResolveBoostXpGoal(dailyXpGoal);
        if (!eligible)
        {
            return BuildBoostStatus(stats.BoostActiveUntil, lessonsToday, xpToday, dailyXpGoal);
        }
        // Каждая активация — фиксированное окно от текущего момента, не накопительно.
        // Сохраняем как Unspecified-DateTime с UTC-тиками — Npgsql 8 в режиме legacy timestamp
        // запишет ровно эти тики в `timestamp without time zone` без сдвига зоны.
        stats.BoostActiveUntil = DateTime.SpecifyKind(now.AddMinutes(minutes), DateTimeKind.Unspecified);
        stats.UpdatedAt = DateTime.SpecifyKind(now, DateTimeKind.Unspecified);
        await _db.SaveChangesAsync();
        return BuildBoostStatus(stats.BoostActiveUntil, lessonsToday, xpToday, dailyXpGoal);
    }

    public async Task<XpBoostStatusDto> GetXpBoostStatusAsync(int userId, int? dailyXpGoal = null)
    {
        var until = await _db.UserStats
            .Where(s => s.UserId == userId)
            .Select(s => s.BoostActiveUntil)
            .FirstOrDefaultAsync();
        var (lessonsToday, xpToday) = await GetTodayProgressAsync(userId);
        return BuildBoostStatus(until, lessonsToday, xpToday, dailyXpGoal);
    }

    private const int BoostMinLessons = 3;

    private const int BoostMinXp = 60;

    private async Task<(int lessons, int xp)> GetTodayProgressAsync(int userId)
    {
        var localNow = GetLocalNow();
        var localDayStart = localNow.Date;
        var localNextDayStart = localDayStart.AddDays(1);
        var utcDayStart = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localDayStart, DateTimeKind.Unspecified), AppTimeZone);
        var utcNextDayStart = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localNextDayStart, DateTimeKind.Unspecified), AppTimeZone);

        var rows = await _db.LessonProgresses
            .Where(p => p.UserId == userId && p.CompletedAt != null && p.CompletedAt >= utcDayStart && p.CompletedAt < utcNextDayStart)
            .Select(p => new { p.XpEarned })
            .ToListAsync();
        return (rows.Count, rows.Sum(x => x.XpEarned));
    }

    private static XpBoostStatusDto BuildBoostStatus(DateTime? until, int lessonsToday = 0, int xpToday = 0, int? dailyXpGoal = null)
    {
        var eligible = lessonsToday >= BoostMinLessons || xpToday >= ResolveBoostXpGoal(dailyXpGoal);
        var now = DateTime.UtcNow;
        // Тики из БД считаем UTC всегда (мы их так и записываем в ActivateXpBoostAsync).
        var untilUtc = until.HasValue ? DateTime.SpecifyKind(until.Value, DateTimeKind.Utc) : (DateTime?)null;
        if (untilUtc is null || untilUtc <= now)
            return new XpBoostStatusDto(false, null, 0, lessonsToday, xpToday, eligible);
        var remaining = (int)Math.Round((untilUtc.Value - now).TotalSeconds);
        // Возвращаем строкой с явным `Z`, чтобы клиент мог парсить как Instant без неожиданностей.
        var activeUntilIso = untilUtc.Value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", System.Globalization.CultureInfo.InvariantCulture);
        return new XpBoostStatusDto(true, activeUntilIso, Math.Max(remaining, 0), lessonsToday, xpToday, eligible);
    }

    private static int ResolveBoostXpGoal(int? dailyXpGoal)
        => dailyXpGoal.HasValue && dailyXpGoal.Value > 0 ? dailyXpGoal.Value : BoostMinXp;

    public async Task<List<QuestionDto>> GetPracticeQuestionsAsync(int userId, int limit)
    {
        // Берём все ID уроков, которые пользователь уже завершил, плюс его
        // прогресс по ним (для оценки «слабости»).
        var lessonProgresses = await _db.LessonProgresses
            .Where(p => p.UserId == userId && p.IsCompleted)
            .Select(p => new { p.LessonId, p.Score, p.MaxScore })
            .ToListAsync();

        if (lessonProgresses.Count == 0) return new List<QuestionDto>();

        // weakness = 1 - score/maxScore. Чем хуже сдан урок, тем выше приоритет
        // его вопросов в практике.
        var weaknessByLesson = lessonProgresses
            .GroupBy(p => p.LessonId)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var maxSum = g.Sum(p => p.MaxScore);
                    if (maxSum <= 0) return 0.5;
                    var scoreSum = g.Sum(p => p.Score);
                    return 1.0 - (double)scoreSum / maxSum;
                });

        var completedLessonIds = weaknessByLesson.Keys.ToList();

        // Исключаем вопросы, на которые пользователь уже ответил правильно
        // в последнюю попытку (на уровне урока или практики).
        var correctlyAnsweredIds = await _db.QuestionAttempts
            .Where(qa => qa.UserId == userId && qa.IsCorrect)
            .Select(qa => qa.QuestionId)
            .ToListAsync();

        var questions = await _db.Questions
            .Include(q => q.Answers)
            .Include(q => q.MatchingPairs)
            .AsSplitQuery()
            .Where(q => completedLessonIds.Contains(q.LessonId) && !correctlyAnsweredIds.Contains(q.Id))
            .ToListAsync();

        var rng = new Random();
        var ordered = questions
            .Select(q => new
            {
                Question = q,
                Weakness = weaknessByLesson.TryGetValue(q.LessonId, out var w) ? w : 0.0,
                Tie = rng.NextDouble()
            })
            .OrderByDescending(x => x.Weakness)
            .ThenBy(x => x.Tie)
            .Select(x => x.Question)
            .Take(limit > 0 ? limit : questions.Count);

        return ordered.Select(q => new QuestionDto(
            q.Id,
            q.Type,
            q.Text,
            q.OrderIndex,
            q.XpValue,
            q.Hint,
            q.ExplanationCorrect,
            q.ExplanationWrong,
            q.Answers
                .OrderBy(a => a.OrderIndex)
                .Select(a => new AnswerDto(a.Id, a.Text, false, a.OrderIndex))
                .ToList(),
            q.MatchingPairs
                .OrderBy(m => m.OrderIndex)
                .Select(m => new MatchingPairDto(m.Id, m.LeftText, m.RightText, m.OrderIndex))
                .ToList()
        )).ToList();
    }

    private static QuestionCheckResultDto EvaluateQuestion(Question question, QuestionAttemptDto answer)
    {
        var isCorrect = false;
        var correctAnswerIds = new List<int>();
        var correctMatchingPairs = new List<MatchingAttemptDto>();

        switch (question.Type)
        {
            case "choice":
            case "truefalse":
            {
                var selectedIds = answer.SelectedAnswerIds?
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList() ?? [];
                correctAnswerIds = question.Answers
                    .Where(a => a.IsCorrect)
                    .Select(a => a.Id)
                    .OrderBy(x => x)
                    .ToList();
                isCorrect = selectedIds.SequenceEqual(correctAnswerIds);
                break;
            }
            case "matching":
            {
                var submittedPairs = answer.MatchingPairs?
                    .OrderBy(x => x.LeftPairId)
                    .ToList() ?? [];
                correctMatchingPairs = question.MatchingPairs
                    .OrderBy(x => x.OrderIndex)
                    .Select(x => new MatchingAttemptDto(x.Id, x.Id))
                    .ToList();
                isCorrect = submittedPairs.Count == correctMatchingPairs.Count
                    && !submittedPairs.Except(correctMatchingPairs).Any();
                break;
            }
        }

        return new QuestionCheckResultDto(
            question.Id,
            isCorrect,
            isCorrect ? question.ExplanationCorrect : question.ExplanationWrong,
            correctAnswerIds,
            correctMatchingPairs
        );
    }


    private static readonly TimeZoneInfo AppTimeZone = ResolveAppTimeZone();

    private static TimeZoneInfo ResolveAppTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");
        }
        catch
        {
            return TimeZoneInfo.Local;
        }
    }

    private static DateTime GetLocalNow() => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, AppTimeZone);

    private static DateOnly GetLocalToday() => DateOnly.FromDateTime(GetLocalNow());
    private static int CalculateLevel(int xp) => xp / 100 + 1;

    private async Task<List<AchievementDto>> CheckAchievementsAsync(int userId, UserStat stats)
    {
        var allAchievements = await _db.Achievements.ToListAsync();

        var earnedIds = await _db.UserAchievements
            .Where(a => a.UserId == userId)
            .Select(a => a.AchievementId)
            .ToListAsync();

        var lessonsCount = await _db.LessonProgresses
            .CountAsync(p => p.UserId == userId && p.IsCompleted);

        var coursesCount = await _db.VCourseProgresses
            .CountAsync(p => p.UserId == userId && p.ProgressPct == 100);

        // avg_score считаем на клиенте (EF не транслирует выражение со Score/MaxScore).
        var attempts = await _db.LessonProgresses
            .Where(p => p.UserId == userId && p.MaxScore > 0)
            .Select(p => new { p.Score, p.MaxScore })
            .ToListAsync();
        var avgScore = attempts.Count == 0
            ? 0
            : (int)Math.Round(attempts.Average(a => (double)a.Score / a.MaxScore * 100.0));

        var newAchievements = new List<AchievementDto>();

        foreach (var ach in allAchievements.Where(a => !earnedIds.Contains(a.Id)))
        {
            var met = ach.ConditionKey switch
            {
                "lessons_done" => lessonsCount >= ach.ConditionValue,
                "streak_days" => stats.StreakDays >= ach.ConditionValue,
                "total_xp" => stats.TotalXp >= ach.ConditionValue,
                "courses_done" => coursesCount >= ach.ConditionValue,
                "avg_score" => avgScore >= ach.ConditionValue,
                _ => false
            };

            if (!met) continue;

            var ua = new UserAchievement
            {
                UserId = userId,
                AchievementId = ach.Id,
                EarnedAt = DateTime.UtcNow
            };
            _db.UserAchievements.Add(ua);
            newAchievements.Add(new AchievementDto(
                ach.Id, ach.Title, ach.Description, ach.Icon, ua.EarnedAt));
        }

        if (newAchievements.Any())
            await _db.SaveChangesAsync();

        return newAchievements;
    }
}



