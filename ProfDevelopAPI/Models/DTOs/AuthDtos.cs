namespace ProfDevelopAPI.Models.DTOs;

public record LoginRequest(
    string Email,
    string Password,
    string? DeviceInfo   // "Avalonia/Windows" или "Android/14"
);

public record RefreshRequest(
    string RefreshToken
);

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    UserProfileDto User
);

public record UserProfileDto(
    int Id,
    string FullName,
    string Email,
    string Role,
    string? PositionTitle,
    string? DepartmentName,
    int TotalXp,
    int Level,
    int StreakDays,
    string? AvatarUrl
);
