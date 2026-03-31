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
            FullName     = request.FullName,
            Email        = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role         = request.Role,
            PositionId   = request.PositionId,
            DepartmentId = request.DepartmentId,
            Phone        = request.Phone,
            IsActive     = true,
            CreatedAt    = DateTime.UtcNow,
            UpdatedAt    = DateTime.UtcNow
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        // Создаём статистику сразу при регистрации
        _db.UserStats.Add(new UserStat { UserId = user.Id });
        await _db.SaveChangesAsync();

        return (await GetByIdAsync(user.Id))!;
    }

    public async Task<UserListDto?> UpdateAsync(int id, UpdateUserRequest request)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return null;

        user.FullName     = request.FullName;
        user.Phone        = request.Phone;
        user.PositionId   = request.PositionId;
        user.DepartmentId = request.DepartmentId;
        user.IsActive     = request.IsActive;
        user.UpdatedAt    = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<bool> DeactivateAsync(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return false;

        user.IsActive  = false;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<AdminStatsDto> GetAdminStatsAsync()
    {
        var s = await _db.VAdminStats.FirstOrDefaultAsync();
        return new AdminStatsDto(
            s?.TotalEmployees,
            s?.ActiveToday,
            s?.PublishedCourses,
            s?.AvgScorePct
        );
    }

    public async Task<List<LeaderboardEntryDto>> GetLeaderboardAsync()
    {
        var rows = await _db.VLeaderboards.ToListAsync();
        return rows.Select(r => new LeaderboardEntryDto(
            r.Rank,
            r.Id         ?? 0,
            r.FullName   ?? "",
            r.AvatarUrl,
            r.PositionTitle,
            r.TotalXp,
            r.Level,
            r.StreakDays
        )).ToList();
    }

    public async Task<List<AchievementDto>> GetUserAchievementsAsync(int userId)
    {
        return await _db.UserAchievements
            .Include(ua => ua.Achievement)
            .Where(ua => ua.UserId == userId)
            .Select(ua => new AchievementDto(
                ua.Achievement.Id,
                ua.Achievement.Title,
                ua.Achievement.Description,
                ua.Achievement.Icon,
                ua.EarnedAt
            ))
            .ToListAsync();
    }

    // ── Маппинг ─────────────────────────────────────────────────────────────
    private static UserListDto MapUserList(VUserFull u) => new(
        u.Id             ?? 0,
        u.FullName       ?? "",
        u.Email          ?? "",
        u.Role           ?? "",
        u.PositionTitle,
        u.DepartmentName,
        u.IsActive       ?? false,
        u.TotalXp,
        u.Level,
        u.StreakDays,
        u.LastActiveDate
    );
}
