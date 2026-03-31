namespace ProfDevelopAPI.Models.DTOs;

// ── Курсы ───────────────────────────────────────────────────────────────────
public record CourseDto(
    int       Id,
    string    Title,
    string?   Description,
    string?   Category,
    string?   CoverUrl,
    bool      IsPublished,
    int       TotalLessons,
    // Прогресс — заполняется только для employee
    int?      CompletedLessons,
    int?      ProgressPct,
    bool?     IsMandatory,
    DateOnly? Deadline
);

public record CreateCourseRequest(
    string  Title,
    string? Description,
    string? Category,
    bool    IsPublished
);

public record UpdateCourseRequest(
    string  Title,
    string? Description,
    string? Category,
    bool    IsPublished
);

// ── Уроки ───────────────────────────────────────────────────────────────────
public record LessonDto(
    int     Id,
    string  Title,
    int     OrderIndex,
    int     XpReward,
    string? Description,
    // Прогресс урока — для мобилки
    bool    IsCompleted,
    bool    IsUnlocked,   // доступен ли (предыдущий пройден)
    int?    Score,
    int?    MaxScore
);

public record CreateLessonRequest(
    int     CourseId,
    string  Title,
    int     XpReward,
    string? Description
);

public record UpdateLessonRequest(
    string  Title,
    int     XpReward,
    string? Description
);

// ── Вопросы ─────────────────────────────────────────────────────────────────
public record QuestionDto(
    int                   Id,
    string                Type,   // choice | truefalse | matching
    string                Text,
    int                   OrderIndex,
    List<AnswerDto>       Answers,
    List<MatchingPairDto> MatchingPairs
);

public record AnswerDto(
    int    Id,
    string Text,
    bool   IsCorrect,   // отправляется ТОЛЬКО в Avalonia (admin), не в мобилку!
    int    OrderIndex
);

public record MatchingPairDto(
    int    Id,
    string LeftText,
    string RightText,
    int    OrderIndex
);

public record CreateQuestionRequest(
    int                      LessonId,
    string                   Type,
    string                   Text,
    List<CreateAnswerRequest> Answers,
    List<CreatePairRequest>   MatchingPairs
);

public record CreateAnswerRequest(
    string Text,
    bool   IsCorrect
);

public record CreatePairRequest(
    string LeftText,
    string RightText
);
