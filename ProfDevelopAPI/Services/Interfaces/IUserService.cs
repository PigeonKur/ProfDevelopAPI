using ProfDevelopAPI.Models.DTOs;

namespace ProfDevelopAPI.Services.Interfaces;

public interface IUserService
{
    Task<List<UserListDto>> GetAllAsync();
    Task<UserListDto?> GetByIdAsync(int id);
    Task<UserListDto> CreateAsync(CreateUserRequest request);
    Task<UserListDto?> UpdateAsync(int id, UpdateUserRequest request);
    Task<bool> DeactivateAsync(int id);
    Task<UserLookupsDto> GetLookupsAsync();
    Task<AdminStatsDto> GetAdminStatsAsync();
    Task<List<LeaderboardEntryDto>> GetLeaderboardAsync(string? tier = null);
    Task<List<AchievementDto>> GetUserAchievementsAsync(int userId, bool includeUnearned = false);
    Task<List<AchievementCatalogDto>> GetAchievementsAsync();
    Task<AchievementCatalogDto> CreateAchievementAsync(CreateAchievementRequest request);
}
