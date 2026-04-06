using System;
using System.Collections.Generic;

namespace ProfDevelopAPI.Models;

/// <summary>
/// Обучающие курсы
/// </summary>
public partial class Course
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string? CoverUrl { get; set; }

    public string? ThemeColor { get; set; }

    public string? IconKey { get; set; }

    public string? Difficulty { get; set; }

    public int EstimatedMinutes { get; set; }

    public int OrderIndex { get; set; }

    public string? Category { get; set; }

    public bool IsPublished { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<CourseAssignment> CourseAssignments { get; set; } = new List<CourseAssignment>();

    public virtual User? CreatedByNavigation { get; set; }

    public virtual ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
}
