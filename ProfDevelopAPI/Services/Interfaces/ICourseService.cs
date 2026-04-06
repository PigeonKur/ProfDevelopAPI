using ProfDevelopAPI.Models.DTOs;

namespace ProfDevelopAPI.Services.Interfaces;

public interface ICourseService
{
    Task<List<CourseDto>> GetAllAsync(int? userId);
    Task<CourseDto?> GetByIdAsync(int courseId, int? userId);
    Task<CourseDto> CreateAsync(CreateCourseRequest request, int createdBy);
    Task<CourseDto?> UpdateAsync(int id, UpdateCourseRequest request);
    Task<bool> DeleteAsync(int id);

    Task<List<LessonDto>> GetLessonsAsync(int courseId, int? userId);
    Task<LessonDto> CreateLessonAsync(CreateLessonRequest request);
    Task<LessonDto?> UpdateLessonAsync(int id, UpdateLessonRequest request);
    Task<bool> ReorderLessonsAsync(int courseId, ReorderItemsRequest request);
    Task<bool> DeleteLessonAsync(int id);

    Task<List<QuestionDto>> GetQuestionsAsync(int lessonId, bool includeCorrect);
    Task<QuestionDto> CreateQuestionAsync(CreateQuestionRequest request);
    Task<QuestionDto?> UpdateQuestionAsync(int id, UpdateQuestionRequest request, bool includeCorrect);
    Task<bool> ReorderQuestionsAsync(int lessonId, ReorderItemsRequest request);
    Task<bool> DeleteQuestionAsync(int id);
}
