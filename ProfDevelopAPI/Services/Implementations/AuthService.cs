using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ProfDevelopAPI.Models;
using ProfDevelopAPI.Models.DTOs;
using ProfDevelopAPI.Services.Interfaces;

namespace ProfDevelopAPI.Services.Implementations;

public class AuthService : IAuthService
{
    private readonly PostgresContext _db;
    private readonly IConfiguration  _cfg;

    public AuthService(PostgresContext db, IConfiguration cfg)
    {
        _db  = db;
        _cfg = cfg;
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request, string ipAddress)
    {
        var user = await _db.Users
            .Include(u => u.Position)
            .Include(u => u.Department)
            .Include(u => u.UserStat)
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive == true);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return null;

        // Отзываем старые токены этого устройства
        var oldTokens = await _db.RefreshTokens
            .Where(t => t.UserId == user.Id
                     && t.DeviceInfo == request.DeviceInfo
                     && t.RevokedAt == null)
            .ToListAsync();
        oldTokens.ForEach(t => t.RevokedAt = DateTime.UtcNow);

        var accessToken  = GenerateAccessToken(user);
        var refreshToken = CreateRefreshToken(user.Id, request.DeviceInfo, ipAddress);
        _db.RefreshTokens.Add(refreshToken);

        await _db.SaveChangesAsync();

        return new AuthResponse(accessToken, refreshToken.Token, MapProfile(user));
    }

    public async Task<AuthResponse?> RefreshAsync(string refreshToken, string ipAddress)
    {
        var token = await _db.RefreshTokens
            .Include(t => t.User).ThenInclude(u => u.Position)
            .Include(t => t.User).ThenInclude(u => u.Department)
            .Include(t => t.User).ThenInclude(u => u.UserStat)
            .FirstOrDefaultAsync(t => t.Token == refreshToken);

        if (token == null || token.RevokedAt != null || token.ExpiresAt < DateTime.UtcNow)
            return null;

        // Ротация токена
        token.RevokedAt  = DateTime.UtcNow;
        var newRefresh   = CreateRefreshToken(token.UserId, token.DeviceInfo, ipAddress);
        _db.RefreshTokens.Add(newRefresh);

        var accessToken = GenerateAccessToken(token.User);
        await _db.SaveChangesAsync();

        return new AuthResponse(accessToken, newRefresh.Token, MapProfile(token.User));
    }

    public async Task RevokeAsync(string refreshToken)
    {
        var token = await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == refreshToken);

        if (token != null)
        {
            token.RevokedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }

    // ── Приватные методы ────────────────────────────────────────────────────
    private string GenerateAccessToken(User user)
    {
        var jwt    = _cfg.GetSection("JwtSettings");
        var key    = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["SecretKey"]!));
        var creds  = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiry = DateTime.UtcNow.AddMinutes(int.Parse(jwt["AccessTokenExpiryMinutes"]!));

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email,          user.Email),
            new Claim(ClaimTypes.Role,           user.Role ?? "employee"),
            new Claim("full_name",               user.FullName)
        };

        var tokenObj = new JwtSecurityToken(
            issuer:             jwt["Issuer"],
            audience:           jwt["Audience"],
            claims:             claims,
            expires:            expiry,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(tokenObj);
    }

    private RefreshToken CreateRefreshToken(int userId, string? deviceInfo, string ipAddress)
    {
        var jwt = _cfg.GetSection("JwtSettings");
        return new RefreshToken
        {
            UserId     = userId,
            Token      = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            DeviceInfo = deviceInfo,
            IpAddress  = ipAddress,
            ExpiresAt  = DateTime.UtcNow.AddDays(int.Parse(jwt["RefreshTokenExpiryDays"]!)),
            CreatedAt  = DateTime.UtcNow
        };
    }

    private static UserProfileDto MapProfile(User u) => new(
        u.Id,
        u.FullName,
        u.Email,
        u.Role ?? "employee",
        u.Position?.Title,
        u.Department?.Name,
        u.UserStat?.TotalXp    ?? 0,
        u.UserStat?.Level      ?? 1,
        u.UserStat?.StreakDays ?? 0,
        u.AvatarUrl
    );
}
