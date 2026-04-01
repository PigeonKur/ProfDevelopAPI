namespace ProfDevelopAPI.Models.DTOs;

public record CourseDto(
    int Id,
    string Title,
    string? Description,
    string? Category,
    string? CoverUrl,
    bool IsPublished,
    int TotalLessons,
    int? CompletedLessons,
    int? ProgressPct,
    bool? IsMandatory,
    DateOnly? Deadline
);

public record CreateCourseRequest(
    string Title,
    string? Description,
    string? Category,
    bool IsPublished
);

public record UpdateCourseRequest(
    string Title,
    string? Description,
    string? Category,
    bool IsPublished
);

public record LessonDto(
    int Id,
    string Title,
    int OrderIndex,
    int XpReward,
    string? Description,
    bool IsCompleted,
    bool IsUnlocked,
    int? Score,
    int? MaxScore
);

public record CreateLessonRequest(
    int CourseId,
    string Title,
    int XpReward,
    string? Description
);

public record UpdateLessonRequest(
    string Title,
    int XpReward,
    string? Description
);

public record QuestionDto(
    int Id,
    string Type,
    string Text,
    int OrderIndex,
    List<AnswerDto> Answers,
    List<MatchingPairDto> MatchingPairs
);

public record AnswerDto(
    int Id,
    string Text,
    bool IsCorrect,   // отправляется ТОЛЬКО в Avalonia, не в мобилку!
    int OrderIndex
);

public record MatchingPairDto(
    int Id,
    string LeftText,
    string RightText,
    int OrderIndex
);

public record CreateQuestionRequest(
    int LessonId,
    string Type,
    string Text,
    List<CreateAnswerRequest> Answers,
    List<CreatePairRequest> MatchingPairs
);

public record UpdateQuestionRequest(
    string Type,
    string Text,
    List<CreateAnswerRequest> Answers,
    List<CreatePairRequest> MatchingPairs
);

public record CreateAnswerRequest(
    string Text,
    bool IsCorrect
);

public record CreatePairRequest(
    string LeftText,
    string RightText
);
