using System;

namespace ProfDevelopAPI.Models;

/// <summary>
/// История попыток ответа пользователя на конкретный вопрос.
/// Используется для практики — вопросы с правильным последним ответом
/// не попадают в выборку.
/// </summary>
public partial class QuestionAttempt
{
    public int UserId { get; set; }

    public int QuestionId { get; set; }

    public bool IsCorrect { get; set; }

    public DateTime LastAttemptAt { get; set; }
}
