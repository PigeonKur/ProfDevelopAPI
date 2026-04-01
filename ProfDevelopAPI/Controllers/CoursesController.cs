using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProfDevelopAPI.Models.DTOs;
using ProfDevelopAPI.Services.Interfaces;

namespace ProfDevelopAPI.Controllers;

[ApiController]
[Route("api/courses")]
[Authorize]
public class CoursesController : ControllerBase
{
    private readonly ICourseService _courses;
    public CoursesController(ICourseService courses) => _courses = courses;

    private int? CurrentUserId => int.TryParse(
        User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    private bool IsAdmin => User.IsInRole("admin") || User.IsInRole("hr");

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await _courses.GetAllAsync(IsAdmin ? null : CurrentUserId));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var course = await _courses.GetByIdAsync(id, IsAdmin ? null : CurrentUserId);
        return course == null ? NotFound() : Ok(course);
    }

    [HttpPost]
    [Authorize(Roles = "admin,hr")]
    public async Task<IActionResult> Create([FromBody] CreateCourseRequest request)
    {
        var course = await _courses.CreateAsync(request, CurrentUserId!.Value);
        return CreatedAtAction(nameof(GetById), new { id = course.Id }, course);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "admin,hr")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCourseRequest request)
    {
        var course = await _courses.UpdateAsync(id, request);
        return course == null ? NotFound() : Ok(course);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Delete(int id)
        => await _courses.DeleteAsync(id) ? NoContent() : NotFound();


    [HttpGet("{courseId}/lessons")]
    public async Task<IActionResult> GetLessons(int courseId)
        => Ok(await _courses.GetLessonsAsync(courseId, IsAdmin ? null : CurrentUserId));

    [HttpPost("lessons")]
    [Authorize(Roles = "admin,hr")]
    public async Task<IActionResult> CreateLesson([FromBody] CreateLessonRequest request)
        => Ok(await _courses.CreateLessonAsync(request));

    [HttpDelete("lessons/{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> DeleteLesson(int id)
        => await _courses.DeleteLessonAsync(id) ? NoContent() : NotFound();

    [HttpGet("lessons/{lessonId}/questions")]
    public async Task<IActionResult> GetQuestions(int lessonId)
        => Ok(await _courses.GetQuestionsAsync(lessonId, includeCorrect: IsAdmin));

    [HttpPost("questions")]
    [Authorize(Roles = "admin,hr")]
    public async Task<IActionResult> CreateQuestion([FromBody] CreateQuestionRequest request)
        => Ok(await _courses.CreateQuestionAsync(request));

    [HttpPut("questions/{id}")]
    [Authorize(Roles = "admin,hr")]
    public async Task<IActionResult> UpdateQuestion(int id, [FromBody] UpdateQuestionRequest request)
    {
        var question = await _courses.UpdateQuestionAsync(id, request, includeCorrect: IsAdmin);
        return question == null ? NotFound() : Ok(question);
    }

    [HttpDelete("questions/{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> DeleteQuestion(int id)
        => await _courses.DeleteQuestionAsync(id) ? NoContent() : NotFound();
}
