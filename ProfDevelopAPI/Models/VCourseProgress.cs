using System;
using System.Collections.Generic;

namespace ProfDevelopAPI.Models;

public partial class VCourseProgress
{
    public int? UserId { get; set; }

    public int? CourseId { get; set; }

    public string? CourseTitle { get; set; }

    public string? Category { get; set; }

    public DateOnly? Deadline { get; set; }

    public bool? IsMandatory { get; set; }

    public long? TotalLessons { get; set; }

    public long? CompletedLessons { get; set; }

    public long? XpEarned { get; set; }

    public decimal? ProgressPct { get; set; }
}
