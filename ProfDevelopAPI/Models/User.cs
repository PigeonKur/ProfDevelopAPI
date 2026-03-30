using System;
using System.Collections.Generic;

namespace ProfDevelopAPI.Models;

/// <summary>
/// Пользователи системы (сотрудники и администраторы)
/// </summary>
public partial class User
{
    public int Id { get; set; }

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    /// <summary>
    /// employee — сотрудник, admin — администратор, hr — HR-отдел
    /// </summary>
    public string Role { get; set; } = null!;

    public int? PositionId { get; set; }

    public int? DepartmentId { get; set; }

    public string? AvatarUrl { get; set; }

    public string? Phone { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<CourseAssignment> CourseAssignmentAssignedByNavigations { get; set; } = new List<CourseAssignment>();

    public virtual ICollection<CourseAssignment> CourseAssignmentUsers { get; set; } = new List<CourseAssignment>();

    public virtual ICollection<Course> Courses { get; set; } = new List<Course>();

    public virtual Department? Department { get; set; }

    public virtual ICollection<LessonProgress> LessonProgresses { get; set; } = new List<LessonProgress>();

    public virtual Position? Position { get; set; }

    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    public virtual ICollection<UserAchievement> UserAchievements { get; set; } = new List<UserAchievement>();

    public virtual UserStat? UserStat { get; set; }
}
