using ProfDevelopAPI.Models.DTOs;

namespace ProfDevelopAPI.Services.Interfaces;

public interface IUserService
{
    Task<List<UserListDto>> GetAllAsync();
    Task<UserListDto?> GetByIdAsync(int id);
    Task<UserListDto> CreateAsync(CreateUserRequest request);
    Task<UserListDto?> UpdateAsync(int id, UpdateUserRequest request);
    Task<bool> DeactivateAsync(int id);
    Task<AdminStatsDto> GetAdminStatsAsync();
    Task<List<LeaderboardEntryDto>> GetLeaderboardAsync();
    Task<List<AchievementDto>> GetUserAchievementsAsync(int userId);
}
