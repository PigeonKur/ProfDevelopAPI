namespace ProfDevelopAPI.Models.DTOs;

public record SubmitProgressRequest(
    int LessonId,
    int Score,
    int MaxScore
);

public record LessonAttemptRequest(
    int LessonId,
    List<QuestionAttemptDto> Answers
);

public record QuestionAttemptDto(
    int QuestionId,
    List<int>? SelectedAnswerIds,
    List<MatchingAttemptDto>? MatchingPairs
);

public record QuestionCheckRequest(
    int QuestionId,
    List<int>? SelectedAnswerIds,
    List<MatchingAttemptDto>? MatchingPairs
);

public record MatchingAttemptDto(
    int LeftPairId,
    int RightPairId
);

public record SubmitProgressResponse(
    bool IsCompleted,
    int XpEarned,
    int TotalXp,
    int NewLevel,
    int StreakDays,
    List<AchievementDto> NewAchievements
);

public record LessonAttemptResultDto(
    bool IsCompleted,
    int Score,
    int MaxScore,
    int XpEarned,
    int TotalXp,
    int NewLevel,
    int StreakDays,
    List<AchievementDto> NewAchievements,
    List<QuestionReviewDto> Questions
);

public record QuestionReviewDto(
    int QuestionId,
    bool IsCorrect,
    string? Explanation,
    List<int> CorrectAnswerIds,
    List<MatchingAttemptDto> CorrectMatchingPairs
);

public record QuestionCheckResultDto(
    int QuestionId,
    bool IsCorrect,
    string? Explanation,
    List<int> CorrectAnswerIds,
    List<MatchingAttemptDto> CorrectMatchingPairs
);

public record AchievementDto(
    int Id,
    string Title,
    string? Description,
    string? Icon,
    DateTime? EarnedAt,
    string? ConditionKey = null,
    int? ConditionValue = null,
    int? CurrentValue = null
);

public record AchievementCatalogDto(
    int Id,
    string Title,
    string? Description,
    string? Icon,
    string ConditionKey,
    int ConditionValue,
    DateTime CreatedAt
);

public record CreateAchievementRequest(
    string Title,
    string? Description,
    string? Icon,
    string ConditionKey,
    int ConditionValue
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
