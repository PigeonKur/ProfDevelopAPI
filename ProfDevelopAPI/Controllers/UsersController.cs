using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProfDevelopAPI.Models.DTOs;
using ProfDevelopAPI.Services.Interfaces;

namespace ProfDevelopAPI.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _users;
    public UsersController(IUserService users) => _users = users;

    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>Список всех сотрудников [admin, hr] — Avalonia</summary>
    [HttpGet]
    [Authorize(Roles = "admin,hr")]
    public async Task<IActionResult> GetAll()
        => Ok(await _users.GetAllAsync());

    /// <summary>Профиль конкретного пользователя</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await _users.GetByIdAsync(id);
        return user == null ? NotFound() : Ok(user);
    }

    /// <summary>Мой профиль — используется в мобилке</summary>
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var user = await _users.GetByIdAsync(CurrentUserId);
        return user == null ? NotFound() : Ok(user);
    }

    /// <summary>Создать сотрудника [admin, hr] — Avalonia</summary>
    [HttpPost]
    [Authorize(Roles = "admin,hr")]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        var user = await _users.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }

    /// <summary>Редактировать сотрудника [admin, hr] — Avalonia</summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "admin,hr")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUserRequest request)
    {
        var user = await _users.UpdateAsync(id, request);
        return user == null ? NotFound() : Ok(user);
    }

    /// <summary>Деактивировать сотрудника [admin]</summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Deactivate(int id)
        => await _users.DeactivateAsync(id) ? NoContent() : NotFound();

    /// <summary>KPI-статистика для дашборда Avalonia [admin, hr]</summary>
    [HttpGet("admin-stats")]
    [Authorize(Roles = "admin,hr")]
    public async Task<IActionResult> AdminStats()
        => Ok(await _users.GetAdminStatsAsync());

    /// <summary>Рейтинг сотрудников — мобилка + Avalonia</summary>
    [HttpGet("leaderboard")]
    public async Task<IActionResult> Leaderboard()
        => Ok(await _users.GetLeaderboardAsync());

    /// <summary>Достижения пользователя</summary>
    [HttpGet("{id}/achievements")]
    public async Task<IActionResult> Achievements(int id)
        => Ok(await _users.GetUserAchievementsAsync(id));
}
