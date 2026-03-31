namespace ProfDevelopAPI.Models.DTOs;

// ── Отправка результата урока (мобилка → API) ───────────────────────────────
public record SubmitProgressRequest(
    int LessonId,
    int Score,
    int MaxScore
);

public record SubmitProgressResponse(
    bool                  IsCompleted,
    int                   XpEarned,
    int                   TotalXp,
    int                   NewLevel,
    int                   StreakDays,
    List<AchievementDto>  NewAchievements  // достижения выданные за этот урок
);

// ── Достижения ──────────────────────────────────────────────────────────────
public record AchievementDto(
    int       Id,
    string    Title,
    string?   Description,
    string?   Icon,
    DateTime? EarnedAt
);

// ── Статистика пользователя ─────────────────────────────────────────────────
public record UserStatsDto(
    int       TotalXp,
    int       Level,
    int       StreakDays,
    int       MaxStreak,
    DateOnly? LastActiveDate
);

// ── Лидерборд ───────────────────────────────────────────────────────────────
public record LeaderboardEntryDto(
    long?   Rank,
    int     UserId,
    string  FullName,
    string? AvatarUrl,
    string? PositionTitle,
    int?    TotalXp,
    int?    Level,
    int?    StreakDays
);

// ── Назначение курса (Avalonia → API) ───────────────────────────────────────
public record AssignCourseRequest(
    int       UserId,
    int       CourseId,
    bool      IsMandatory,
    DateOnly? Deadline
);
