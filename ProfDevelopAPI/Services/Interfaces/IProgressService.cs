using ProfDevelopAPI.Models.DTOs;

namespace ProfDevelopAPI.Services.Interfaces;

public interface IProgressService
{
    Task<SubmitProgressResponse> SubmitAsync(int userId, SubmitProgressRequest request);
    Task<List<CourseDto>> GetUserCoursesAsync(int userId);
    Task<bool> AssignCourseAsync(AssignCourseRequest request, int assignedBy);
}
