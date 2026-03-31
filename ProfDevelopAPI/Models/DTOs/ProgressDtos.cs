namespace ProfDevelopAPI.Models.DTOs;

public record SubmitProgressRequest(
    int LessonId,
    int Score,
    int MaxScore
);

public record SubmitProgressResponse(
    bool IsCompleted,
    int XpEarned,
    int TotalXp,
    int NewLevel,
    int StreakDays,
    List<AchievementDto> NewAchievements
);

public record AchievementDto(
    int Id,
    string Title,
    string? Description,
    string? Icon,
    DateTime? EarnedAt
);

public record UserStatsDto(
    int TotalXp,
    int Level,
    int StreakDays,
    int MaxStreak,
    DateOnly? LastActiveDate
);

public record LeaderboardEntryDto(
    long? Rank,
    int UserId,
    string FullName,
    string? AvatarUrl,
    string? PositionTitle,
    int? TotalXp,
    int? Level,
    int? StreakDays
);

public record AssignCourseRequest(
    int UserId,
    int CourseId,
    bool IsMandatory,
    DateOnly? Deadline
);
