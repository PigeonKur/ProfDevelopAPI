using Microsoft.EntityFrameworkCore;
using ProfDevelopAPI.Models;
using ProfDevelopAPI.Models.DTOs;
using ProfDevelopAPI.Services.Interfaces;

namespace ProfDevelopAPI.Services.Implementations;

public class CourseService : ICourseService
{
    private readonly PostgresContext _db;
    public CourseService(PostgresContext db) => _db = db;

    public async Task<List<CourseDto>> GetAllAsync(int? userId)
    {
        var courses = await _db.Courses
            .Include(c => c.Lessons)
            .OrderBy(c => c.OrderIndex)
            .ThenBy(c => c.CreatedAt)
            .ToListAsync();

        return courses.Select(c => MapCourse(c)).ToList();
    }

    public async Task<CourseDto?> GetByIdAsync(int courseId, int? userId)
    {
        var course = await _db.Courses
            .Include(c => c.Lessons)
            .FirstOrDefaultAsync(c => c.Id == courseId);

        return course == null ? null : MapCourse(course);
    }

    public async Task<CourseDto> CreateAsync(CreateCourseRequest request, int createdBy)
    {
        var maxOrder = await _db.Courses.MaxAsync(c => (int?)c.OrderIndex) ?? 0;

        var course = new Course
        {
            Title = request.Title,
            Description = request.Description,
            Category = request.Category,
            CoverUrl = request.CoverUrl,
            ThemeColor = request.ThemeColor,
            IconKey = request.IconKey,
            Difficulty = request.Difficulty,
            EstimatedMinutes = request.EstimatedMinutes,
            OrderIndex = maxOrder + 1,
            IsPublished = request.IsPublished,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Courses.Add(course);
        await _db.SaveChangesAsync();
        return MapCourse(course);
    }

    public async Task<CourseDto?> UpdateAsync(int id, UpdateCourseRequest request)
    {
        var course = await _db.Courses
            .Include(c => c.Lessons)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (course == null) return null;

        course.Title = request.Title;
        course.Description = request.Description;
        course.Category = request.Category;
        course.CoverUrl = request.CoverUrl;
        course.ThemeColor = request.ThemeColor;
        course.IconKey = request.IconKey;
        course.Difficulty = request.Difficulty;
        course.EstimatedMinutes = request.EstimatedMinutes;
        course.IsPublished = request.IsPublished;
        course.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return MapCourse(course);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var course = await _db.Courses.FindAsync(id);
        if (course == null) return false;
        _db.Courses.Remove(course);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<LessonDto>> GetLessonsAsync(int courseId, int? userId)
    {
        var lessons = await _db.Lessons
            .Where(l => l.CourseId == courseId)
            .OrderBy(l => l.OrderIndex)
            .ToListAsync();

        List<LessonProgress> progresses = new();
        if (userId.HasValue)
        {
            var lessonIds = lessons.Select(l => l.Id).ToList();
            progresses = await _db.LessonProgresses
                .Where(p => p.UserId == userId && lessonIds.Contains(p.LessonId))
                .ToListAsync();
        }

        return lessons.Select((l, i) =>
        {
            var prog = progresses.FirstOrDefault(p => p.LessonId == l.Id);
            var requiredLesson = l.RequiredLessonId.HasValue
                ? lessons.FirstOrDefault(x => x.Id == l.RequiredLessonId.Value)
                : null;
            var isUnlocked = userId == null
                || (l.RequiredLessonId.HasValue
                    ? progresses.Any(p => p.LessonId == l.RequiredLessonId.Value && p.IsCompleted)
                    : !l.IsLockedByDefault
                      || i == 0
                      || progresses.Any(p => p.LessonId == lessons[i - 1].Id && p.IsCompleted));

            return MapLesson(l, prog, isUnlocked, requiredLesson?.Title);
        }).ToList();
    }

    public async Task<LessonDto> CreateLessonAsync(CreateLessonRequest request)
    {
        var maxOrder = await _db.Lessons
            .Where(l => l.CourseId == request.CourseId)
            .MaxAsync(l => (int?)l.OrderIndex) ?? 0;

        var lesson = new Lesson
        {
            CourseId = request.CourseId,
            Title = request.Title,
            OrderIndex = maxOrder + 1,
            XpReward = request.XpReward,
            Description = request.Description,
            LessonType = request.LessonType,
            EstimatedMinutes = request.EstimatedMinutes,
            IsLockedByDefault = request.IsLockedByDefault,
            RequiredLessonId = request.RequiredLessonId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Lessons.Add(lesson);
        await _db.SaveChangesAsync();

        return MapLesson(lesson, null, !lesson.IsLockedByDefault, null);
    }

    public async Task<LessonDto?> UpdateLessonAsync(int id, UpdateLessonRequest request)
    {
        var lesson = await _db.Lessons.FirstOrDefaultAsync(l => l.Id == id);
        if (lesson == null) return null;

        lesson.Title = request.Title;
        lesson.XpReward = request.XpReward;
        lesson.Description = request.Description;
        lesson.LessonType = request.LessonType;
        lesson.EstimatedMinutes = request.EstimatedMinutes;
        lesson.IsLockedByDefault = request.IsLockedByDefault;
        lesson.RequiredLessonId = request.RequiredLessonId;
        lesson.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        string? requiredLessonTitle = null;
        if (lesson.RequiredLessonId.HasValue)
        {
            requiredLessonTitle = await _db.Lessons
                .Where(l => l.Id == lesson.RequiredLessonId.Value)
                .Select(l => l.Title)
                .FirstOrDefaultAsync();
        }

        return MapLesson(lesson, null, !lesson.IsLockedByDefault, requiredLessonTitle);
    }

    public async Task<bool> ReorderLessonsAsync(int courseId, ReorderItemsRequest request)
    {
        var lessons = await _db.Lessons
            .Where(l => l.CourseId == courseId)
            .OrderBy(l => l.OrderIndex)
            .ToListAsync();

        if (lessons.Count == 0 || request.OrderedIds.Count != lessons.Count)
            return false;

        var lessonIds = lessons.Select(l => l.Id).OrderBy(id => id).ToList();
        if (!lessonIds.SequenceEqual(request.OrderedIds.OrderBy(id => id)))
            return false;

        await using var transaction = await _db.Database.BeginTransactionAsync();

        // Break unique index conflicts while two lessons swap positions.
        for (var i = 0; i < request.OrderedIds.Count; i++)
        {
            var lesson = lessons.First(l => l.Id == request.OrderedIds[i]);
            lesson.OrderIndex = request.OrderedIds.Count + i + 1;
            lesson.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        for (var i = 0; i < request.OrderedIds.Count; i++)
        {
            var lesson = lessons.First(l => l.Id == request.OrderedIds[i]);
            lesson.OrderIndex = i + 1;
            lesson.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        return true;
    }

    public async Task<bool> DeleteLessonAsync(int id)
    {
        var lesson = await _db.Lessons.FindAsync(id);
        if (lesson == null) return false;
        _db.Lessons.Remove(lesson);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<QuestionDto>> GetQuestionsAsync(int lessonId, bool includeCorrect)
    {
        var questions = await _db.Questions
            .Include(q => q.Answers)
            .Include(q => q.MatchingPairs)
            .AsSplitQuery()
            .Where(q => q.LessonId == lessonId)
            .OrderBy(q => q.OrderIndex)
            .ToListAsync();

        return questions.Select(q => new QuestionDto(
            q.Id,
            q.Type,
            q.Text,
            q.OrderIndex,
            q.XpValue,
            q.Hint,
            q.ExplanationCorrect,
            q.ExplanationWrong,
            q.Answers
                .OrderBy(a => a.OrderIndex)
                .Select(a => new AnswerDto(
                    a.Id,
                    a.Text,
                    includeCorrect ? a.IsCorrect : false,
                    a.OrderIndex
                )).ToList(),
            q.MatchingPairs
                .OrderBy(m => m.OrderIndex)
                .Select(m => new MatchingPairDto(
                    m.Id, m.LeftText, m.RightText, m.OrderIndex
                )).ToList()
        )).ToList();
    }

    public async Task<QuestionDto> CreateQuestionAsync(CreateQuestionRequest request)
    {
        var maxOrder = await _db.Questions
            .Where(q => q.LessonId == request.LessonId)
            .MaxAsync(q => (int?)q.OrderIndex) ?? 0;

        var question = new Question
        {
            LessonId = request.LessonId,
            Type = request.Type,
            Text = request.Text,
            OrderIndex = maxOrder + 1,
            XpValue = request.XpValue,
            Hint = request.Hint,
            ExplanationCorrect = request.ExplanationCorrect,
            ExplanationWrong = request.ExplanationWrong,
            CreatedAt = DateTime.UtcNow
        };
        _db.Questions.Add(question);
        await _db.SaveChangesAsync();

        for (int i = 0; i < request.Answers.Count; i++)
        {
            _db.Answers.Add(new Answer
            {
                QuestionId = question.Id,
                Text = request.Answers[i].Text,
                IsCorrect = request.Answers[i].IsCorrect,
                OrderIndex = i + 1
            });
        }

        for (int i = 0; i < request.MatchingPairs.Count; i++)
        {
            _db.MatchingPairs.Add(new MatchingPair
            {
                QuestionId = question.Id,
                LeftText = request.MatchingPairs[i].LeftText,
                RightText = request.MatchingPairs[i].RightText,
                OrderIndex = i + 1
            });
        }

        await _db.SaveChangesAsync();

        return new QuestionDto(
            question.Id, question.Type, question.Text, question.OrderIndex,
            question.XpValue, question.Hint, question.ExplanationCorrect, question.ExplanationWrong,
            request.Answers.Select((a, i) =>
                new AnswerDto(0, a.Text, a.IsCorrect, i + 1)).ToList(),
            request.MatchingPairs.Select((p, i) =>
                new MatchingPairDto(0, p.LeftText, p.RightText, i + 1)).ToList()
        );
    }

    public async Task<QuestionDto?> UpdateQuestionAsync(int id, UpdateQuestionRequest request, bool includeCorrect)
    {
        var question = await _db.Questions
            .Include(q => q.Answers)
            .Include(q => q.MatchingPairs)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (question == null) return null;

        question.Type = request.Type;
        question.Text = request.Text;
        question.XpValue = request.XpValue;
        question.Hint = request.Hint;
        question.ExplanationCorrect = request.ExplanationCorrect;
        question.ExplanationWrong = request.ExplanationWrong;

        _db.Answers.RemoveRange(question.Answers);
        _db.MatchingPairs.RemoveRange(question.MatchingPairs);

        for (int i = 0; i < request.Answers.Count; i++)
        {
            _db.Answers.Add(new Answer
            {
                QuestionId = question.Id,
                Text = request.Answers[i].Text,
                IsCorrect = request.Answers[i].IsCorrect,
                OrderIndex = i + 1
            });
        }

        for (int i = 0; i < request.MatchingPairs.Count; i++)
        {
            _db.MatchingPairs.Add(new MatchingPair
            {
                QuestionId = question.Id,
                LeftText = request.MatchingPairs[i].LeftText,
                RightText = request.MatchingPairs[i].RightText,
                OrderIndex = i + 1
            });
        }

        await _db.SaveChangesAsync();

        var updated = await _db.Questions
            .Include(q => q.Answers)
            .Include(q => q.MatchingPairs)
            .AsSplitQuery()
            .FirstAsync(q => q.Id == id);

        return new QuestionDto(
            updated.Id,
            updated.Type,
            updated.Text,
            updated.OrderIndex,
            updated.XpValue,
            updated.Hint,
            updated.ExplanationCorrect,
            updated.ExplanationWrong,
            updated.Answers
                .OrderBy(a => a.OrderIndex)
                .Select(a => new AnswerDto(
                    a.Id,
                    a.Text,
                    includeCorrect ? a.IsCorrect : false,
                    a.OrderIndex
                )).ToList(),
            updated.MatchingPairs
                .OrderBy(m => m.OrderIndex)
                .Select(m => new MatchingPairDto(
                    m.Id, m.LeftText, m.RightText, m.OrderIndex
                )).ToList()
        );
    }

    public async Task<bool> ReorderQuestionsAsync(int lessonId, ReorderItemsRequest request)
    {
        var questions = await _db.Questions
            .Where(q => q.LessonId == lessonId)
            .OrderBy(q => q.OrderIndex)
            .ToListAsync();

        if (questions.Count == 0 || request.OrderedIds.Count != questions.Count)
            return false;

        var questionIds = questions.Select(q => q.Id).OrderBy(id => id).ToList();
        if (!questionIds.SequenceEqual(request.OrderedIds.OrderBy(id => id)))
            return false;

        for (var i = 0; i < request.OrderedIds.Count; i++)
        {
            var question = questions.First(q => q.Id == request.OrderedIds[i]);
            question.OrderIndex = i + 1;
        }

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteQuestionAsync(int id)
    {
        var q = await _db.Questions.FindAsync(id);
        if (q == null) return false;
        _db.Questions.Remove(q);
        await _db.SaveChangesAsync();
        return true;
    }

    private static CourseDto MapCourse(Course c) => new(
        c.Id, c.OrderIndex, c.Title, c.Description, c.Category, c.CoverUrl, c.ThemeColor, c.IconKey, c.Difficulty, c.EstimatedMinutes,
        c.IsPublished,
        c.Lessons.Count,
        null, null, null, null
    );

    private static LessonDto MapLesson(Lesson lesson, LessonProgress? progress, bool isUnlocked, string? requiredLessonTitle) => new(
        lesson.Id,
        lesson.Title,
        lesson.OrderIndex,
        lesson.XpReward,
        lesson.Description,
        lesson.LessonType,
        lesson.EstimatedMinutes,
        lesson.IsLockedByDefault,
        lesson.RequiredLessonId,
        requiredLessonTitle,
        progress?.IsCompleted ?? false,
        isUnlocked,
        progress?.Score,
        progress?.MaxScore
    );
}
