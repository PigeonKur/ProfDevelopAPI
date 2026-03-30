using System;
using System.Collections.Generic;

namespace ProfDevelopAPI.Models;

/// <summary>
/// Справочник достижений
/// </summary>
public partial class Achievement
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string? Icon { get; set; }

    /// <summary>
    /// Тип условия: lessons_done / streak_days / total_xp / avg_score / courses_done
    /// </summary>
    public string ConditionKey { get; set; } = null!;

    /// <summary>
    /// Пороговое значение для выдачи
    /// </summary>
    public int ConditionValue { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<UserAchievement> UserAchievements { get; set; } = new List<UserAchievement>();
}
