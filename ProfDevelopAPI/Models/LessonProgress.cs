using System;
using System.Collections.Generic;

namespace ProfDevelopAPI.Models;

/// <summary>
/// Прогресс сотрудника по урокам
/// </summary>
public partial class LessonProgress
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int LessonId { get; set; }

    public int Score { get; set; }

    public int MaxScore { get; set; }

    public int XpEarned { get; set; }

    public int Attempts { get; set; }

    public bool IsCompleted { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Lesson Lesson { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
