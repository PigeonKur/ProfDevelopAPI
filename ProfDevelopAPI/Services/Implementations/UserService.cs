using Microsoft.EntityFrameworkCore;
using ProfDevelopAPI.Models;
using ProfDevelopAPI.Models.DTOs;
using ProfDevelopAPI.Services.Interfaces;

namespace ProfDevelopAPI.Services.Implementations;

public class UserService : IUserService
{
    private readonly PostgresContext _db;
    public UserService(PostgresContext db) => _db = db;

    public async Task<List<UserListDto>> GetAllAsync()
    {
        var users = await _db.VUserFulls.ToListAsync();
        return users.Select(MapUserList).ToList();
    }

    public async Task<UserListDto?> GetByIdAsync(int id)
    {
        var u = await _db.VUserFulls.FirstOrDefaultAsync(u => u.Id == id);
        return u == null ? null : MapUserList(u);
    }

    public async Task<UserListDto> CreateAsync(CreateUserRequest request)
    {
        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = request.Role,
            PositionId = request.PositionId,
            DepartmentId = request.DepartmentId,
            Phone = request.Phone,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        _db.UserStats.Add(new UserStat { UserId = user.Id });
        await _db.SaveChangesAsync();

        return (await GetByIdAsync(user.Id))!;
    }

    public async Task<UserListDto?> UpdateAsync(int id, UpdateUserRequest request)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return null;

        user.FullName = request.FullName;
        user.Phone = request.Phone;
        user.PositionId = request.PositionId;
        user.DepartmentId = request.DepartmentId;
        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<bool> DeactivateAsync(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return false;

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<UserLookupsDto> GetLookupsAsync()
    {
        var departments = await _db.Departments
            .OrderBy(d => d.Name)
            .Select(d => new LookupItemDto(d.Id, d.Name))
            .ToListAsync();

        var positions = await _db.Positions
            .Where(p => p.IsActive)
            .OrderBy(p => p.Title)
            .Select(p => new LookupItemDto(p.Id, p.Title))
            .ToListAsync();

        return new UserLookupsDto(departments, positions);
    }

    public async Task<AdminStatsDto> GetAdminStatsAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var totalEmployees = await _db.Users.CountAsync(u => u.Role == "employee");
        var activeToday = await _db.UserStats.CountAsync(s => s.LastActiveDate == today);
        var publishedCourses = await _db.Courses.CountAsync(c => c.IsPublished);

        var avgScorePct = await _db.LessonProgresses
            .Where(p => p.MaxScore > 0)
            .Select(p => (decimal?)p.Score * 100m / p.MaxScore)
            .AverageAsync();

        return new AdminStatsDto(
            totalEmployees,
            activeToday,
            publishedCourses,
            avgScorePct
        );
    }

    public async Task<List<LeaderboardEntryDto>> GetLeaderboardAsync(string? tier = null)
    {
        var rows = await _db.VLeaderboards.ToListAsync();

        // Недельная XP: считаем сумму xp_earned за уроки, завершённые с
        // понедельника 00:00 UTC. Сбрасывается еженедельно.
        var now = DateTime.UtcNow;
        var dow = (int)now.DayOfWeek; // Sunday=0..Saturday=6
        var daysSinceMonday = (dow + 6) % 7;
        var monday = now.Date.AddDays(-daysSinceMonday);
        var weeklyXpByUser = await _db.LessonProgresses
            .Where(p => p.CompletedAt != null && p.CompletedAt >= monday)
            .GroupBy(p => p.UserId)
            .Select(g => new { UserId = g.Key, Xp = g.Sum(p => p.XpEarned) })
            .ToDictionaryAsync(x => x.UserId, x => x.Xp);

        var entries = rows
            .Select(r =>
            {
                var userId = r.Id ?? 0;
                var totalXp = r.TotalXp ?? 0;
                var weekly = weeklyXpByUser.TryGetValue(userId, out var w) ? w : 0;
                return new LeaderboardEntryDto(
                    r.Rank,
                    userId,
                    r.FullName ?? "",
                    r.AvatarUrl,
                    r.PositionTitle,
                    r.TotalXp,
                    r.Level,
                    r.StreakDays,
                    Tier: ComputeTier(totalXp),
                    WeeklyXp: weekly
                );
            })
            .ToList();

        if (!string.IsNullOrWhiteSpace(tier))
        {
            var t = tier.Trim().ToLowerInvariant();
            entries = entries
                .Where(e => string.Equals(e.Tier, t, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(e => e.WeeklyXp)
                .ThenByDescending(e => e.TotalXp)
                .Take(30)
                .Select((e, i) => e with { Rank = i + 1 })
                .ToList();
        }

        return entries;
    }

    private static string ComputeTier(int totalXp) => totalXp switch
    {
        >= 15000 => "legendary",
        >= 5000 => "diamond",
        >= 1500 => "gold",
        >= 500 => "silver",
        _ => "bronze"
    };

    public async Task<List<AchievementDto>> GetUserAchievementsAsync(int userId, bool includeUnearned = false)
    {
        var earnedMap = await _db.UserAchievements
            .Where(ua => ua.UserId == userId)
            .ToDictionaryAsync(ua => ua.AchievementId, ua => (DateTime?)ua.EarnedAt);

        // Без includeUnearned — старое поведение: только полученные, без новых полей.
        // Так не сломаем десктопную админку, которая ожидает плоский список «получено».
        if (!includeUnearned)
        {
            return await _db.UserAchievements
                .Include(ua => ua.Achievement)
                .Where(ua => ua.UserId == userId)
                .Select(ua => new AchievementDto(
                    ua.Achievement.Id,
                    ua.Achievement.Title,
                    ua.Achievement.Description,
                    ua.Achievement.Icon,
                    ua.EarnedAt,
                    ua.Achievement.ConditionKey,
                    ua.Achievement.ConditionValue,
                    ua.Achievement.ConditionValue
                ))
                .ToListAsync();
        }

        // С includeUnearned (для мобилки) — все ачивки + текущий прогресс.
        var allAchievements = await _db.Achievements
            .OrderBy(a => a.ConditionValue)
            .ThenBy(a => a.Title)
            .ToListAsync();

        var stats = await _db.UserStats.FirstOrDefaultAsync(s => s.UserId == userId);
        var lessonsDone = await _db.LessonProgresses
            .CountAsync(p => p.UserId == userId && p.IsCompleted);
        var coursesDone = await _db.VCourseProgresses
            .CountAsync(p => p.UserId == userId && p.ProgressPct == 100);
        // EF Core не транслирует Select+DefaultIfEmpty+AverageAsync — считаем
        // среднее на клиенте после простого SELECT (Score, MaxScore).
        var attempts = await _db.LessonProgresses
            .Where(p => p.UserId == userId && p.MaxScore > 0)
            .Select(p => new { p.Score, p.MaxScore })
            .ToListAsync();
        var avgScore = attempts.Count == 0
            ? 0
            : (int)Math.Round(attempts.Average(a => (double)a.Score / a.MaxScore * 100.0));

        // Ретроспективно выдаём награды, по которым условие уже выполнено,
        // но запись в user_achievements ещё не появилась (например, ачивки
        // могли быть добавлены позже, чем юзер достиг условия).
        var now = DateTime.UtcNow;
        var newlyEarned = false;
        foreach (var ach in allAchievements)
        {
            if (earnedMap.ContainsKey(ach.Id)) continue;
            int? cur = ach.ConditionKey switch
            {
                "lessons_done" => lessonsDone,
                "streak_days" => stats?.StreakDays ?? 0,
                "total_xp" => stats?.TotalXp ?? 0,
                "courses_done" => coursesDone,
                "avg_score" => avgScore,
                _ => null
            };
            if (cur is null) continue;
            if (cur.Value < ach.ConditionValue) continue;

            _db.UserAchievements.Add(new Models.UserAchievement
            {
                UserId = userId,
                AchievementId = ach.Id,
                EarnedAt = now
            });
            earnedMap[ach.Id] = now;
            newlyEarned = true;
        }
        if (newlyEarned) await _db.SaveChangesAsync();

        return allAchievements.Select(ach =>
        {
            int? current = ach.ConditionKey switch
            {
                "lessons_done" => lessonsDone,
                "streak_days" => stats?.StreakDays ?? 0,
                "total_xp" => stats?.TotalXp ?? 0,
                "courses_done" => coursesDone,
                "avg_score" => avgScore,
                _ => null
            };

            earnedMap.TryGetValue(ach.Id, out var earnedAt);

            return new AchievementDto(
                ach.Id,
                ach.Title,
                ach.Description,
                ach.Icon,
                earnedAt,
                ach.ConditionKey,
                ach.ConditionValue,
                current
            );
        }).ToList();
    }

    public async Task<List<AchievementCatalogDto>> GetAchievementsAsync()
    {
        return await _db.Achievements
            .OrderByDescending(a => a.CreatedAt)
            .ThenBy(a => a.Title)
            .Select(a => new AchievementCatalogDto(
                a.Id,
                a.Title,
                a.Description,
                a.Icon,
                a.ConditionKey,
                a.ConditionValue,
                a.CreatedAt
            ))
            .ToListAsync();
    }

    public async Task<AchievementCatalogDto> CreateAchievementAsync(CreateAchievementRequest request)
    {
        var achievement = new Achievement
        {
            Title = request.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Icon = string.IsNullOrWhiteSpace(request.Icon) ? null : request.Icon.Trim(),
            ConditionKey = request.ConditionKey.Trim(),
            ConditionValue = request.ConditionValue,
            CreatedAt = DateTime.UtcNow
        };

        _db.Achievements.Add(achievement);
        await _db.SaveChangesAsync();

        return new AchievementCatalogDto(
            achievement.Id,
            achievement.Title,
            achievement.Description,
            achievement.Icon,
            achievement.ConditionKey,
            achievement.ConditionValue,
            achievement.CreatedAt
        );
    }

    private static UserListDto MapUserList(VUserFull u) => new(
        u.Id ?? 0,
        u.FullName ?? "",
        u.Email ?? "",
        u.Role ?? "",
        u.PositionTitle,
        u.DepartmentName,
        u.IsActive ?? false,
        u.TotalXp,
        u.Level,
        u.StreakDays,
        u.LastActiveDate
    );
}
