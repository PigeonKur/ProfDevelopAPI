using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ProfDevelopAPI.Models;

public partial class PostgresContext : DbContext
{
    public PostgresContext()
    {
    }

    public PostgresContext(DbContextOptions<PostgresContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Achievement> Achievements { get; set; }

    public virtual DbSet<Answer> Answers { get; set; }

    public virtual DbSet<Course> Courses { get; set; }

    public virtual DbSet<CourseAssignment> CourseAssignments { get; set; }

    public virtual DbSet<Department> Departments { get; set; }

    public virtual DbSet<Lesson> Lessons { get; set; }

    public virtual DbSet<LessonProgress> LessonProgresses { get; set; }

    public virtual DbSet<MatchingPair> MatchingPairs { get; set; }

    public virtual DbSet<Position> Positions { get; set; }

    public virtual DbSet<Question> Questions { get; set; }

    public virtual DbSet<QuestionAttempt> QuestionAttempts { get; set; }

    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserAchievement> UserAchievements { get; set; }

    public virtual DbSet<UserStat> UserStats { get; set; }

    public virtual DbSet<VAdminStat> VAdminStats { get; set; }

    public virtual DbSet<VCourseProgress> VCourseProgresses { get; set; }

    public virtual DbSet<VLeaderboard> VLeaderboards { get; set; }

    public virtual DbSet<VUserFull> VUserFulls { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("pg_catalog", "adminpack");

        modelBuilder.Entity<Achievement>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("achievements_pkey");

            entity.ToTable("achievements", "profDevelop", tb => tb.HasComment("Справочник достижений"));

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ConditionKey)
                .HasMaxLength(50)
                .HasComment("Тип условия: lessons_done / streak_days / total_xp / avg_score / courses_done")
                .HasColumnName("condition_key");
            entity.Property(e => e.ConditionValue)
                .HasComment("Пороговое значение для выдачи")
                .HasColumnName("condition_value");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Icon)
                .HasMaxLength(10)
                .HasColumnName("icon");
            entity.Property(e => e.Title)
                .HasMaxLength(150)
                .HasColumnName("title");
        });

        modelBuilder.Entity<Answer>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("answers_pkey");

            entity.ToTable("answers", "profDevelop", tb => tb.HasComment("Варианты ответов для вопросов типа choice и truefalse"));

            entity.HasIndex(e => e.QuestionId, "idx_answers_question");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.IsCorrect)
                .HasDefaultValue(false)
                .HasColumnName("is_correct");
            entity.Property(e => e.OrderIndex)
                .HasDefaultValue(1)
                .HasColumnName("order_index");
            entity.Property(e => e.QuestionId).HasColumnName("question_id");
            entity.Property(e => e.Text).HasColumnName("text");

            entity.HasOne(d => d.Question).WithMany(p => p.Answers)
                .HasForeignKey(d => d.QuestionId)
                .HasConstraintName("answers_question_id_fkey");
        });

        modelBuilder.Entity<Course>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("courses_pkey");

            entity.ToTable("courses", "profDevelop", tb => tb.HasComment("Обучающие курсы"));

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Category)
                .HasMaxLength(100)
                .HasColumnName("category");
            entity.Property(e => e.CoverUrl)
                .HasMaxLength(500)
                .HasColumnName("cover_url");
            entity.Property(e => e.Difficulty)
                .HasMaxLength(30)
                .HasColumnName("difficulty");
            entity.Property(e => e.EstimatedMinutes)
                .HasDefaultValue(0)
                .HasColumnName("estimated_minutes");
            entity.Property(e => e.IconKey)
                .HasMaxLength(50)
                .HasColumnName("icon_key");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IsPublished)
                .HasDefaultValue(false)
                .HasColumnName("is_published");
            entity.Property(e => e.OrderIndex)
                .HasDefaultValue(1)
                .HasColumnName("order_index");
            entity.Property(e => e.ThemeColor)
                .HasMaxLength(20)
                .HasColumnName("theme_color");
            entity.Property(e => e.Title)
                .HasMaxLength(200)
                .HasColumnName("title");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Courses)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("courses_created_by_fkey");
        });

        modelBuilder.Entity<CourseAssignment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("course_assignments_pkey");

            entity.ToTable("course_assignments", "profDevelop", tb => tb.HasComment("Назначение курсов сотрудникам"));

            entity.HasIndex(e => new { e.UserId, e.CourseId }, "course_assignments_user_id_course_id_key").IsUnique();

            entity.HasIndex(e => e.CourseId, "idx_assignments_course");

            entity.HasIndex(e => e.UserId, "idx_assignments_user");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AssignedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("assigned_at");
            entity.Property(e => e.AssignedBy).HasColumnName("assigned_by");
            entity.Property(e => e.CourseId).HasColumnName("course_id");
            entity.Property(e => e.Deadline).HasColumnName("deadline");
            entity.Property(e => e.IsMandatory)
                .HasDefaultValue(false)
                .HasColumnName("is_mandatory");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.AssignedByNavigation).WithMany(p => p.CourseAssignmentAssignedByNavigations)
                .HasForeignKey(d => d.AssignedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("course_assignments_assigned_by_fkey");

            entity.HasOne(d => d.Course).WithMany(p => p.CourseAssignments)
                .HasForeignKey(d => d.CourseId)
                .HasConstraintName("course_assignments_course_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.CourseAssignmentUsers)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("course_assignments_user_id_fkey");
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("departments_pkey");

            entity.ToTable("departments", "profDevelop", tb => tb.HasComment("Подразделения министерства / организации"));

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Lesson>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("lessons_pkey");

            entity.ToTable("lessons", "profDevelop", tb => tb.HasComment("Уроки внутри курса"));

            entity.HasIndex(e => e.CourseId, "idx_lessons_course");

            entity.HasIndex(e => new { e.CourseId, e.OrderIndex }, "lessons_course_id_order_index_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CourseId).HasColumnName("course_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.EstimatedMinutes)
                .HasDefaultValue(5)
                .HasColumnName("estimated_minutes");
            entity.Property(e => e.IsLockedByDefault)
                .HasDefaultValue(false)
                .HasColumnName("is_locked_by_default");
            entity.Property(e => e.LessonType)
                .HasMaxLength(20)
                .HasDefaultValue("quiz")
                .HasColumnName("lesson_type");
            entity.Property(e => e.OrderIndex)
                .HasDefaultValue(1)
                .HasColumnName("order_index");
            entity.Property(e => e.RequiredLessonId).HasColumnName("required_lesson_id");
            entity.Property(e => e.Title)
                .HasMaxLength(200)
                .HasColumnName("title");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.XpReward)
                .HasDefaultValue(10)
                .HasColumnName("xp_reward");

            entity.HasOne(d => d.Course).WithMany(p => p.Lessons)
                .HasForeignKey(d => d.CourseId)
                .HasConstraintName("lessons_course_id_fkey");
        });

        modelBuilder.Entity<LessonProgress>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("lesson_progress_pkey");

            entity.ToTable("lesson_progress", "profDevelop", tb => tb.HasComment("Прогресс сотрудника по урокам"));

            entity.HasIndex(e => e.LessonId, "idx_progress_lesson");

            entity.HasIndex(e => e.UserId, "idx_progress_user");

            entity.HasIndex(e => new { e.UserId, e.LessonId }, "lesson_progress_user_id_lesson_id_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Attempts)
                .HasDefaultValue(1)
                .HasColumnName("attempts");
            entity.Property(e => e.CompletedAt).HasColumnName("completed_at");
            entity.Property(e => e.IsCompleted)
                .HasDefaultValue(false)
                .HasColumnName("is_completed");
            entity.Property(e => e.LessonId).HasColumnName("lesson_id");
            entity.Property(e => e.MaxScore)
                .HasDefaultValue(0)
                .HasColumnName("max_score");
            entity.Property(e => e.Score)
                .HasDefaultValue(0)
                .HasColumnName("score");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.XpEarned)
                .HasDefaultValue(0)
                .HasColumnName("xp_earned");

            entity.HasOne(d => d.Lesson).WithMany(p => p.LessonProgresses)
                .HasForeignKey(d => d.LessonId)
                .HasConstraintName("lesson_progress_lesson_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.LessonProgresses)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("lesson_progress_user_id_fkey");
        });

        modelBuilder.Entity<MatchingPair>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("matching_pairs_pkey");

            entity.ToTable("matching_pairs", "profDevelop", tb => tb.HasComment("Пары для вопросов типа matching"));

            entity.HasIndex(e => e.QuestionId, "idx_matching_question");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.LeftText)
                .HasMaxLength(300)
                .HasColumnName("left_text");
            entity.Property(e => e.OrderIndex)
                .HasDefaultValue(1)
                .HasColumnName("order_index");
            entity.Property(e => e.QuestionId).HasColumnName("question_id");
            entity.Property(e => e.RightText)
                .HasMaxLength(300)
                .HasColumnName("right_text");

            entity.HasOne(d => d.Question).WithMany(p => p.MatchingPairs)
                .HasForeignKey(d => d.QuestionId)
                .HasConstraintName("matching_pairs_question_id_fkey");
        });

        modelBuilder.Entity<Position>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("positions_pkey");

            entity.ToTable("positions", "profDevelop", tb => tb.HasComment("Должности госслужбы и НКО"));

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DepartmentId).HasColumnName("department_id");
            entity.Property(e => e.Grade)
                .HasMaxLength(100)
                .HasColumnName("grade");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Rank)
                .HasMaxLength(100)
                .HasColumnName("rank");
            entity.Property(e => e.Title)
                .HasMaxLength(200)
                .HasColumnName("title");

            entity.HasOne(d => d.Department).WithMany(p => p.Positions)
                .HasForeignKey(d => d.DepartmentId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("positions_department_id_fkey");
        });

        modelBuilder.Entity<Question>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("questions_pkey");

            entity.ToTable("questions", "profDevelop", tb => tb.HasComment("Вопросы урока. type: choice — выбор ответа, truefalse — правда/ложь, matching — соответствие"));

            entity.HasIndex(e => e.LessonId, "idx_questions_lesson");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.LessonId).HasColumnName("lesson_id");
            entity.Property(e => e.OrderIndex)
                .HasDefaultValue(1)
                .HasColumnName("order_index");
            entity.Property(e => e.ExplanationCorrect).HasColumnName("explanation_correct");
            entity.Property(e => e.ExplanationWrong).HasColumnName("explanation_wrong");
            entity.Property(e => e.Hint).HasColumnName("hint");
            entity.Property(e => e.Text).HasColumnName("text");
            entity.Property(e => e.Type)
                .HasMaxLength(20)
                .HasColumnName("type");
            entity.Property(e => e.XpValue)
                .HasDefaultValue(10)
                .HasColumnName("xp_value");

            entity.HasOne(d => d.Lesson).WithMany(p => p.Questions)
                .HasForeignKey(d => d.LessonId)
                .HasConstraintName("questions_lesson_id_fkey");
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("refresh_tokens_pkey");

            entity.ToTable("refresh_tokens", "profDevelop", tb => tb.HasComment("Refresh-токены для Avalonia и Android клиентов"));

            entity.HasIndex(e => e.Token, "idx_refresh_tokens_token");

            entity.HasIndex(e => e.UserId, "idx_refresh_tokens_user");

            entity.HasIndex(e => e.Token, "refresh_tokens_token_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DeviceInfo)
                .HasMaxLength(200)
                .HasColumnName("device_info");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.IpAddress)
                .HasMaxLength(50)
                .HasColumnName("ip_address");
            entity.Property(e => e.RevokedAt).HasColumnName("revoked_at");
            entity.Property(e => e.Token)
                .HasMaxLength(500)
                .HasColumnName("token");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.RefreshTokens)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("refresh_tokens_user_id_fkey");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("users_pkey");

            entity.ToTable("users", "profDevelop", tb => tb.HasComment("Пользователи системы (сотрудники и администраторы)"));

            entity.HasIndex(e => e.DepartmentId, "idx_users_department");

            entity.HasIndex(e => e.Email, "idx_users_email");

            entity.HasIndex(e => e.Role, "idx_users_role");

            entity.HasIndex(e => e.Email, "users_email_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AvatarUrl)
                .HasMaxLength(500)
                .HasColumnName("avatar_url");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DepartmentId).HasColumnName("department_id");
            entity.Property(e => e.Email)
                .HasMaxLength(150)
                .HasColumnName("email");
            entity.Property(e => e.FullName)
                .HasMaxLength(150)
                .HasColumnName("full_name");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("password_hash");
            entity.Property(e => e.Phone)
                .HasMaxLength(30)
                .HasColumnName("phone");
            entity.Property(e => e.PositionId).HasColumnName("position_id");
            entity.Property(e => e.Role)
                .HasMaxLength(20)
                .HasDefaultValueSql("'employee'::character varying")
                .HasComment("employee — сотрудник, admin — администратор, hr — HR-отдел")
                .HasColumnName("role");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Department).WithMany(p => p.Users)
                .HasForeignKey(d => d.DepartmentId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("users_department_id_fkey");

            entity.HasOne(d => d.Position).WithMany(p => p.Users)
                .HasForeignKey(d => d.PositionId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("users_position_id_fkey");
        });

        modelBuilder.Entity<UserAchievement>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_achievements_pkey");

            entity.ToTable("user_achievements", "profDevelop", tb => tb.HasComment("Достижения, выданные пользователям"));

            entity.HasIndex(e => e.UserId, "idx_user_ach_user");

            entity.HasIndex(e => new { e.UserId, e.AchievementId }, "user_achievements_user_id_achievement_id_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AchievementId).HasColumnName("achievement_id");
            entity.Property(e => e.EarnedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("earned_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Achievement).WithMany(p => p.UserAchievements)
                .HasForeignKey(d => d.AchievementId)
                .HasConstraintName("user_achievements_achievement_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.UserAchievements)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("user_achievements_user_id_fkey");
        });

        modelBuilder.Entity<QuestionAttempt>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.QuestionId }).HasName("question_attempts_pkey");

            entity.ToTable("question_attempts", "profDevelop");

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.QuestionId).HasColumnName("question_id");
            entity.Property(e => e.IsCorrect).HasColumnName("is_correct");
            entity.Property(e => e.LastAttemptAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("last_attempt_at");
        });

        modelBuilder.Entity<UserStat>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("user_stats_pkey");

            entity.ToTable("user_stats", "profDevelop", tb => tb.HasComment("Игровая статистика пользователя (XP, уровень, streak)"));

            entity.Property(e => e.UserId)
                .ValueGeneratedNever()
                .HasColumnName("user_id");
            entity.Property(e => e.LastActiveDate).HasColumnName("last_active_date");
            entity.Property(e => e.BoostActiveUntil).HasColumnName("boost_active_until");
            entity.Property(e => e.Level)
                .HasDefaultValue(1)
                .HasColumnName("level");
            entity.Property(e => e.MaxStreak)
                .HasDefaultValue(0)
                .HasColumnName("max_streak");
            entity.Property(e => e.StreakDays)
                .HasDefaultValue(0)
                .HasColumnName("streak_days");
            entity.Property(e => e.TotalXp)
                .HasDefaultValue(0)
                .HasColumnName("total_xp");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.User).WithOne(p => p.UserStat)
                .HasForeignKey<UserStat>(d => d.UserId)
                .HasConstraintName("user_stats_user_id_fkey");
        });

        modelBuilder.Entity<VAdminStat>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_admin_stats", "profDevelop");

            entity.Property(e => e.ActiveToday).HasColumnName("active_today");
            entity.Property(e => e.AvgScorePct).HasColumnName("avg_score_pct");
            entity.Property(e => e.PublishedCourses).HasColumnName("published_courses");
            entity.Property(e => e.TotalEmployees).HasColumnName("total_employees");
        });

        modelBuilder.Entity<VCourseProgress>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_course_progress", "profDevelop");

            entity.Property(e => e.Category)
                .HasMaxLength(100)
                .HasColumnName("category");
            entity.Property(e => e.CompletedLessons).HasColumnName("completed_lessons");
            entity.Property(e => e.CourseId).HasColumnName("course_id");
            entity.Property(e => e.CourseTitle)
                .HasMaxLength(200)
                .HasColumnName("course_title");
            entity.Property(e => e.Deadline).HasColumnName("deadline");
            entity.Property(e => e.IsMandatory).HasColumnName("is_mandatory");
            entity.Property(e => e.ProgressPct).HasColumnName("progress_pct");
            entity.Property(e => e.TotalLessons).HasColumnName("total_lessons");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.XpEarned).HasColumnName("xp_earned");
        });

        modelBuilder.Entity<VLeaderboard>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_leaderboard", "profDevelop");

            entity.Property(e => e.AvatarUrl)
                .HasMaxLength(500)
                .HasColumnName("avatar_url");
            entity.Property(e => e.FullName)
                .HasMaxLength(150)
                .HasColumnName("full_name");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Level).HasColumnName("level");
            entity.Property(e => e.PositionTitle)
                .HasMaxLength(200)
                .HasColumnName("position_title");
            entity.Property(e => e.Rank).HasColumnName("rank");
            entity.Property(e => e.StreakDays).HasColumnName("streak_days");
            entity.Property(e => e.TotalXp).HasColumnName("total_xp");
        });

        modelBuilder.Entity<VUserFull>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_user_full", "profDevelop");

            entity.Property(e => e.AvatarUrl)
                .HasMaxLength(500)
                .HasColumnName("avatar_url");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.DepartmentName)
                .HasMaxLength(200)
                .HasColumnName("department_name");
            entity.Property(e => e.Email)
                .HasMaxLength(150)
                .HasColumnName("email");
            entity.Property(e => e.FullName)
                .HasMaxLength(150)
                .HasColumnName("full_name");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.LastActiveDate).HasColumnName("last_active_date");
            entity.Property(e => e.Level).HasColumnName("level");
            entity.Property(e => e.MaxStreak).HasColumnName("max_streak");
            entity.Property(e => e.Phone)
                .HasMaxLength(30)
                .HasColumnName("phone");
            entity.Property(e => e.PositionGrade)
                .HasMaxLength(100)
                .HasColumnName("position_grade");
            entity.Property(e => e.PositionRank)
                .HasMaxLength(100)
                .HasColumnName("position_rank");
            entity.Property(e => e.PositionTitle)
                .HasMaxLength(200)
                .HasColumnName("position_title");
            entity.Property(e => e.Role)
                .HasMaxLength(20)
                .HasColumnName("role");
            entity.Property(e => e.StreakDays).HasColumnName("streak_days");
            entity.Property(e => e.TotalXp).HasColumnName("total_xp");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
