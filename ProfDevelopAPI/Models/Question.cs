using System;
using System.Collections.Generic;

namespace ProfDevelopAPI.Models;

/// <summary>
/// Вопросы урока. type: choice — выбор ответа, truefalse — правда/ложь, matching — соответствие
/// </summary>
public partial class Question
{
    public int Id { get; set; }

    public int LessonId { get; set; }

    public string Type { get; set; } = null!;

    public string Text { get; set; } = null!;

    public int OrderIndex { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Answer> Answers { get; set; } = new List<Answer>();

    public virtual Lesson Lesson { get; set; } = null!;

    public virtual ICollection<MatchingPair> MatchingPairs { get; set; } = new List<MatchingPair>();
}
