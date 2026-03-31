using ProfDevelopAPI.Models.DTOs;

namespace ProfDevelopAPI.Services.Interfaces;

public interface IAuthService
{
    Task<AuthResponse?> LoginAsync(LoginRequest request, string ipAddress);
    Task<AuthResponse?> RefreshAsync(string refreshToken, string ipAddress);
    Task RevokeAsync(string refreshToken);
}
