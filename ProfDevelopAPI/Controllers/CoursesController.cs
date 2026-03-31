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

    // Avalonia (admin/hr) получает полные данные; мобилка — только своё
    private bool IsAdmin => User.IsInRole("admin") || User.IsInRole("hr");

    /// <summary>Список всех курсов</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await _courses.GetAllAsync(IsAdmin ? null : CurrentUserId));

    /// <summary>Курс по ID</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var course = await _courses.GetByIdAsync(id, IsAdmin ? null : CurrentUserId);
        return course == null ? NotFound() : Ok(course);
    }

    /// <summary>Создать курс [admin, hr]</summary>
    [HttpPost]
    [Authorize(Roles = "admin,hr")]
    public async Task<IActionResult> Create([FromBody] CreateCourseRequest request)
    {
        var course = await _courses.CreateAsync(request, CurrentUserId!.Value);
        return CreatedAtAction(nameof(GetById), new { id = course.Id }, course);
    }

    /// <summary>Обновить курс [admin, hr]</summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "admin,hr")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCourseRequest request)
    {
        var course = await _courses.UpdateAsync(id, request);
        return course == null ? NotFound() : Ok(course);
    }

    /// <summary>Удалить курс [admin]</summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Delete(int id)
        => await _courses.DeleteAsync(id) ? NoContent() : NotFound();

    // ── Уроки ───────────────────────────────────────────────────────────────

    /// <summary>Список уроков курса</summary>
    [HttpGet("{courseId}/lessons")]
    public async Task<IActionResult> GetLessons(int courseId)
        => Ok(await _courses.GetLessonsAsync(courseId, IsAdmin ? null : CurrentUserId));

    /// <summary>Создать урок [admin, hr]</summary>
    [HttpPost("lessons")]
    [Authorize(Roles = "admin,hr")]
    public async Task<IActionResult> CreateLesson([FromBody] CreateLessonRequest request)
        => Ok(await _courses.CreateLessonAsync(request));

    /// <summary>Удалить урок [admin]</summary>
    [HttpDelete("lessons/{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> DeleteLesson(int id)
        => await _courses.DeleteLessonAsync(id) ? NoContent() : NotFound();

    // ── Вопросы ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Вопросы урока.
    /// Avalonia получает is_correct=true, мобилка — всегда false (правильный ответ скрыт)
    /// </summary>
    [HttpGet("lessons/{lessonId}/questions")]
    public async Task<IActionResult> GetQuestions(int lessonId)
        => Ok(await _courses.GetQuestionsAsync(lessonId, includeCorrect: IsAdmin));

    /// <summary>Создать вопрос [admin, hr]</summary>
    [HttpPost("questions")]
    [Authorize(Roles = "admin,hr")]
    public async Task<IActionResult> CreateQuestion([FromBody] CreateQuestionRequest request)
        => Ok(await _courses.CreateQuestionAsync(request));

    /// <summary>Удалить вопрос [admin]</summary>
    [HttpDelete("questions/{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> DeleteQuestion(int id)
        => await _courses.DeleteQuestionAsync(id) ? NoContent() : NotFound();
}
