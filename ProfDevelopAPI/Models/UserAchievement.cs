using System;
using System.Collections.Generic;

namespace ProfDevelopAPI.Models;

/// <summary>
/// Достижения, выданные пользователям
/// </summary>
public partial class UserAchievement
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int AchievementId { get; set; }

    public DateTime EarnedAt { get; set; }

    public virtual Achievement Achievement { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
