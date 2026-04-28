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

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
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

    public async Task<QuestionCheckResultDto> CheckQuestionAsync(QuestionCheckRequest request)
    {
        var question = await _db.Questions
            .Include(q => q.Answers)
            .Include(q => q.MatchingPairs)
            .FirstOrDefaultAsync(q => q.Id == request.QuestionId)
            ?? throw new KeyNotFoundException("Вопрос не найден");

        return EvaluateQuestion(
            question,
            new QuestionAttemptDto(
                request.QuestionId,
                request.SelectedAnswerIds,
                request.MatchingPairs
            )
        );
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

        foreach (var question in lesson.Questions.OrderBy(q => q.OrderIndex))
        {
            var answer = request.Answers.FirstOrDefault(x => x.QuestionId == question.Id)
                ?? new QuestionAttemptDto(question.Id, null, null);

            var evaluation = EvaluateQuestion(question, answer);
            if (evaluation.IsCorrect)
                score++;

            review.Add(new QuestionReviewDto(
                evaluation.QuestionId,
                evaluation.IsCorrect,
                evaluation.Explanation,
                evaluation.CorrectAnswerIds,
                evaluation.CorrectMatchingPairs
            ));
        }

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

    public async Task<List<QuestionDto>> GetPracticeQuestionsAsync(int userId, int limit)
    {
        // Берём все ID уроков, которые пользователь уже завершил.
        var completedLessonIds = await _db.LessonProgresses
            .Where(p => p.UserId == userId && p.IsCompleted)
            .Select(p => p.LessonId)
            .ToListAsync();

        if (completedLessonIds.Count == 0) return new List<QuestionDto>();

        var questions = await _db.Questions
            .Include(q => q.Answers)
            .Include(q => q.MatchingPairs)
            .AsSplitQuery()
            .Where(q => completedLessonIds.Contains(q.LessonId))
            .ToListAsync();

        var rng = new Random();
        var shuffled = questions.OrderBy(_ => rng.Next()).Take(limit > 0 ? limit : questions.Count);

        return shuffled.Select(q => new QuestionDto(
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

        var newAchievements = new List<AchievementDto>();

        foreach (var ach in allAchievements.Where(a => !earnedIds.Contains(a.Id)))
        {
            var met = ach.ConditionKey switch
            {
                "lessons_done" => lessonsCount >= ach.ConditionValue,
                "streak_days" => stats.StreakDays >= ach.ConditionValue,
                "total_xp" => stats.TotalXp >= ach.ConditionValue,
                "courses_done" => coursesCount >= ach.ConditionValue,
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
