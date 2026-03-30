using System;
using System.Collections.Generic;

namespace ProfDevelopAPI.Models;

public partial class VLeaderboard
{
    public long? Rank { get; set; }

    public int? Id { get; set; }

    public string? FullName { get; set; }

    public string? AvatarUrl { get; set; }

    public string? PositionTitle { get; set; }

    public int? TotalXp { get; set; }

    public int? Level { get; set; }

    public int? StreakDays { get; set; }
}
