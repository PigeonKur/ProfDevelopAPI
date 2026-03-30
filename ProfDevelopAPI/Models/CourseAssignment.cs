using System;
using System.Collections.Generic;

namespace ProfDevelopAPI.Models;

/// <summary>
/// Назначение курсов сотрудникам
/// </summary>
public partial class CourseAssignment
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int CourseId { get; set; }

    public int? AssignedBy { get; set; }

    public DateTime AssignedAt { get; set; }

    public DateOnly? Deadline { get; set; }

    public bool IsMandatory { get; set; }

    public virtual User? AssignedByNavigation { get; set; }

    public virtual Course Course { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
