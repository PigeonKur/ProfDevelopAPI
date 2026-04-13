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

    [HttpGet]
    [Authorize(Roles = "admin,hr")]
    public async Task<IActionResult> GetAll()
        => Ok(await _users.GetAllAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await _users.GetByIdAsync(id);
        return user == null ? NotFound() : Ok(user);
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var user = await _users.GetByIdAsync(CurrentUserId);
        return user == null ? NotFound() : Ok(user);
    }

    [HttpPost]
    [Authorize(Roles = "admin,hr")]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        var user = await _users.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "admin,hr")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUserRequest request)
    {
        var user = await _users.UpdateAsync(id, request);
        return user == null ? NotFound() : Ok(user);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Deactivate(int id)
        => await _users.DeactivateAsync(id) ? NoContent() : NotFound();

    [HttpGet("lookups")]
    [Authorize(Roles = "admin,hr")]
    public async Task<IActionResult> Lookups()
        => Ok(await _users.GetLookupsAsync());

    [HttpGet("admin-stats")]
    [Authorize(Roles = "admin,hr")]
    public async Task<IActionResult> AdminStats()
        => Ok(await _users.GetAdminStatsAsync());

    [HttpGet("leaderboard")]
    public async Task<IActionResult> Leaderboard()
        => Ok(await _users.GetLeaderboardAsync());

    [HttpGet("{id}/achievements")]
    public async Task<IActionResult> Achievements(int id)
        => Ok(await _users.GetUserAchievementsAsync(id));

    [HttpGet("achievements/catalog")]
    [Authorize(Roles = "admin,hr")]
    public async Task<IActionResult> AchievementCatalog()
        => Ok(await _users.GetAchievementsAsync());

    [HttpPost("achievements")]
    [Authorize(Roles = "admin,hr")]
    public async Task<IActionResult> CreateAchievement([FromBody] CreateAchievementRequest request)
        => Ok(await _users.CreateAchievementAsync(request));
}
