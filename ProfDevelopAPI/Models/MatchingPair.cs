using System;
using System.Collections.Generic;

namespace ProfDevelopAPI.Models;

/// <summary>
/// Пары для вопросов типа matching
/// </summary>
public partial class MatchingPair
{
    public int Id { get; set; }

    public int QuestionId { get; set; }

    public string LeftText { get; set; } = null!;

    public string RightText { get; set; } = null!;

    public int OrderIndex { get; set; }

    public virtual Question Question { get; set; } = null!;
}
