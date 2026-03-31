using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProfDevelopAPI.Models.DTOs;
using ProfDevelopAPI.Services.Interfaces;

namespace ProfDevelopAPI.Controllers;

[ApiController]
[Route("api/progress")]
[Authorize]
public class ProgressController : ControllerBase
{
    private readonly IProgressService _progress;
    public ProgressController(IProgressService progress) => _progress = progress;

    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>
    /// Отправить результат урока (мобилка → API).
    /// Считает XP, streak, уровень, проверяет достижения.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Submit([FromBody] SubmitProgressRequest request)
    {
        try
        {
            var result = await _progress.SubmitAsync(CurrentUserId, request);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>Назначенные курсы с прогрессом текущего пользователя</summary>
    [HttpGet("my-courses")]
    public async Task<IActionResult> MyCourses()
        => Ok(await _progress.GetUserCoursesAsync(CurrentUserId));

    /// <summary>Назначить курс сотруднику [admin, hr] — используется в Avalonia</summary>
    [HttpPost("assign")]
    [Authorize(Roles = "admin,hr")]
    public async Task<IActionResult> Assign([FromBody] AssignCourseRequest request)
    {
        var ok = await _progress.AssignCourseAsync(request, CurrentUserId);
        return ok
            ? Ok(new { message = "Курс успешно назначен" })
            : Conflict(new { message = "Этот курс уже назначен данному сотруднику" });
    }
}
