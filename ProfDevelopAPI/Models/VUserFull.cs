using System;
using System.Collections.Generic;

namespace ProfDevelopAPI.Models;

public partial class VUserFull
{
    public int? Id { get; set; }

    public string? FullName { get; set; }

    public string? Email { get; set; }

    public string? Role { get; set; }

    public bool? IsActive { get; set; }

    public string? AvatarUrl { get; set; }

    public string? Phone { get; set; }

    public string? PositionTitle { get; set; }

    public string? PositionGrade { get; set; }

    public string? PositionRank { get; set; }

    public string? DepartmentName { get; set; }

    public int? TotalXp { get; set; }

    public int? Level { get; set; }

    public int? StreakDays { get; set; }

    public int? MaxStreak { get; set; }

    public DateOnly? LastActiveDate { get; set; }

    public DateTime? CreatedAt { get; set; }
}
