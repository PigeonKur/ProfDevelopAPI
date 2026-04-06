namespace ProfDevelopAPI.Models.DTOs;

public record UserListDto(
    int Id,
    string FullName,
    string Email,
    string Role,
    string? PositionTitle,
    string? DepartmentName,
    bool IsActive,
    int? TotalXp,
    int? Level,
    int? StreakDays,
    DateOnly? LastActiveDate
);

public record CreateUserRequest(
    string FullName,
    string Email,
    string Password,
    string Role,
    int? PositionId,
    int? DepartmentId,
    string? Phone
);

public record UpdateUserRequest(
    string FullName,
    string? Phone,
    int? PositionId,
    int? DepartmentId,
    bool IsActive
);

public record LookupItemDto(
    int Id,
    string Name
);

public record UserLookupsDto(
    List<LookupItemDto> Departments,
    List<LookupItemDto> Positions
);

public record AdminStatsDto(
    long? TotalEmployees,
    long? ActiveToday,
    long? PublishedCourses,
    decimal? AvgScorePct
);
