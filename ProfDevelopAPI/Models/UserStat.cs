using System;
using System.Collections.Generic;

namespace ProfDevelopAPI.Models;

/// <summary>
/// Игровая статистика пользователя (XP, уровень, streak)
/// </summary>
public partial class UserStat
{
    public int UserId { get; set; }

    public int TotalXp { get; set; }

    public int Level { get; set; }

    public int StreakDays { get; set; }

    public int MaxStreak { get; set; }

    public DateOnly? LastActiveDate { get; set; }

    public DateTime? BoostActiveUntil { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
