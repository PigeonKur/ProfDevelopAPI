using System;
using System.Collections.Generic;

namespace ProfDevelopAPI.Models;

/// <summary>
/// Должности госслужбы и НКО
/// </summary>
public partial class Position
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;

    public string? Grade { get; set; }

    public string? Rank { get; set; }

    public int? DepartmentId { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Department? Department { get; set; }

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
