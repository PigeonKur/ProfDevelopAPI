using System;
using System.Collections.Generic;

namespace ProfDevelopAPI.Models;

/// <summary>
/// Refresh-токены для Avalonia и Android клиентов
/// </summary>
public partial class RefreshToken
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Token { get; set; } = null!;

    public string? DeviceInfo { get; set; }

    public string? IpAddress { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
