using ProfDevelopAPI.Models.DTOs;

namespace ProfDevelopAPI.Services.Interfaces;

public interface IProgressService
{
    Task<SubmitProgressResponse> SubmitAsync(int userId, SubmitProgressRequest request);
    Task<QuestionCheckResultDto> CheckQuestionAsync(QuestionCheckRequest request);
    Task<QuestionCheckResultDto> CheckQuestionAsync(int userId, QuestionCheckRequest request);
    Task<LessonAttemptResultDto> SubmitLessonAttemptAsync(int userId, LessonAttemptRequest request);
    Task<List<CourseDto>> GetUserCoursesAsync(int userId);
    Task<bool> AssignCourseAsync(AssignCourseRequest request, int assignedBy);
    Task<List<QuestionDto>> GetPracticeQuestionsAsync(int userId, int limit);
    Task<XpBoostStatusDto> ActivateXpBoostAsync(int userId, int durationMinutes);
    Task<XpBoostStatusDto> GetXpBoostStatusAsync(int userId);
}
