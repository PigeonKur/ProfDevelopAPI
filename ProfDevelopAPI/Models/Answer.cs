using System;
using System.Collections.Generic;

namespace ProfDevelopAPI.Models;

/// <summary>
/// Варианты ответов для вопросов типа choice и truefalse
/// </summary>
public partial class Answer
{
    public int Id { get; set; }

    public int QuestionId { get; set; }

    public string Text { get; set; } = null!;

    public bool IsCorrect { get; set; }

    public int OrderIndex { get; set; }

    public virtual Question Question { get; set; } = null!;
}
