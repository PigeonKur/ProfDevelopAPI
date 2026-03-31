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

        // Порог прохождения — 70%
        var passed   = request.MaxScore > 0
                    && (request.Score * 100 / request.MaxScore) >= 70;
        var xpEarned = passed ? lesson.XpReward ?? 10 : 0;

        // Создаём или обновляем прогресс
        var progress = await _db.LessonProgresses
            .FirstOrDefaultAsync(p => p.UserId == userId && p.LessonId == request.LessonId);

        if (progress == null)
        {
            progress = new LessonProgress
            {
                UserId   = userId,
                LessonId = request.LessonId,
                Attempts = 1
            };
            _db.LessonProgresses.Add(progress);
        }
        else
        {
            progress.Attempts++;
        }

        // Записываем только если результат лучше предыдущего
        if (!progress.IsCompleted || request.Score > progress.Score)
        {
            progress.Score       = request.Score;
            progress.MaxScore    = request.MaxScore;
            progress.XpEarned   = xpEarned;
            progress.IsCompleted = passed;
            if (passed) progress.CompletedAt = DateTime.UtcNow;
            progress.UpdatedAt = DateTime.UtcNow;
        }

        // Обновляем статистику пользователя
        var stats = await _db.UserStats.FindAsync(userId);
        if (stats == null)
        {
            stats = new UserStat { UserId = userId };
            _db.UserStats.Add(stats);
        }

        if (passed)
        {
            stats.TotalXp += xpEarned;
            stats.Level    = CalculateLevel(stats.TotalXp ?? 0);
        }

        // Обновляем streak
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (stats.LastActiveDate == null || stats.LastActiveDate < today)
        {
            var yesterday   = today.AddDays(-1);
            stats.StreakDays = (stats.LastActiveDate == yesterday)
                ? (stats.StreakDays ?? 0) + 1
                : 1;

            if (stats.StreakDays > (stats.MaxStreak ?? 0))
                stats.MaxStreak = stats.StreakDays;

            stats.LastActiveDate = today;
        }
        stats.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        // Проверяем новые достижения
        var newAchievements = await CheckAchievementsAsync(userId, stats);

        return new SubmitProgressResponse(
            passed,
            xpEarned,
            stats.TotalXp    ?? 0,
            stats.Level      ?? 1,
            stats.StreakDays ?? 0,
            newAchievements
        );
    }

    public async Task<List<CourseDto>> GetUserCoursesAsync(int userId)
    {
        var progresses = await _db.VCourseProgresses
            .Where(p => p.UserId == userId)
            .ToListAsync();

        return progresses.Select(p => new CourseDto(
            p.CourseId        ?? 0,
            p.CourseTitle     ?? "",
            null,
            p.Category,
            null,
            true,
            (int)(p.TotalLessons     ?? 0),
            (int)(p.CompletedLessons ?? 0),
            (int)(p.ProgressPct      ?? 0),
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
            UserId      = request.UserId,
            CourseId    = request.CourseId,
            AssignedBy  = assignedBy,
            IsMandatory = request.IsMandatory,
            Deadline    = request.Deadline,
            AssignedAt  = DateTime.UtcNow
        });

        // Создаём статистику если ещё нет
        if (!await _db.UserStats.AnyAsync(s => s.UserId == request.UserId))
            _db.UserStats.Add(new UserStat { UserId = request.UserId });

        await _db.SaveChangesAsync();
        return true;
    }

    // ── Вспомогательные ─────────────────────────────────────────────────────
    private static int CalculateLevel(int xp) => xp / 100 + 1;

    private async Task<List<AchievementDto>> CheckAchievementsAsync(int userId, UserStat stats)
    {
        var allAchievements = await _db.Achievements.ToListAsync();

        var earnedIds = await _db.UserAchievements
            .Where(a => a.UserId == userId)
            .Select(a => a.AchievementId)
            .ToListAsync();

        var lessonsCount = await _db.LessonProgresses
            .CountAsync(p => p.UserId == userId && p.IsCompleted == true);

        var coursesCount = await _db.VCourseProgresses
            .CountAsync(p => p.UserId == userId && p.ProgressPct == 100);

        var newAchievements = new List<AchievementDto>();

        foreach (var ach in allAchievements.Where(a => !earnedIds.Contains(a.Id)))
        {
            var met = ach.ConditionKey switch
            {
                "lessons_done" => lessonsCount          >= ach.ConditionValue,
                "streak_days"  => (stats.StreakDays ?? 0) >= ach.ConditionValue,
                "total_xp"     => (stats.TotalXp    ?? 0) >= ach.ConditionValue,
                "courses_done" => coursesCount          >= ach.ConditionValue,
                _              => false
            };

            if (!met) continue;

            var ua = new UserAchievement
            {
                UserId        = userId,
                AchievementId = ach.Id,
                EarnedAt      = DateTime.UtcNow
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
