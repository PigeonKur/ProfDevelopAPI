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
            .OrderBy(c => c.CreatedAt)
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
        var course = new Course
        {
            Title = request.Title,
            Description = request.Description,
            Category = request.Category,
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
            var isUnlocked = i == 0
                || progresses.Any(p => p.LessonId == lessons[i - 1].Id && p.IsCompleted);

            return new LessonDto(
                l.Id,
                l.Title,
                l.OrderIndex,
                l.XpReward,
                l.Description,
                prog?.IsCompleted ?? false,
                isUnlocked,
                prog?.Score,
                prog?.MaxScore
            );
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
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Lessons.Add(lesson);
        await _db.SaveChangesAsync();

        return new LessonDto(
            lesson.Id, lesson.Title,
            lesson.OrderIndex, lesson.XpReward,
            lesson.Description, false, false, null, null
        );
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
            .Where(q => q.LessonId == lessonId)
            .OrderBy(q => q.OrderIndex)
            .ToListAsync();

        return questions.Select(q => new QuestionDto(
            q.Id,
            q.Type,
            q.Text,
            q.OrderIndex,
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
            request.Answers.Select((a, i) =>
                new AnswerDto(0, a.Text, a.IsCorrect, i + 1)).ToList(),
            request.MatchingPairs.Select((p, i) =>
                new MatchingPairDto(0, p.LeftText, p.RightText, i + 1)).ToList()
        );
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
        c.Id, c.Title, c.Description, c.Category, c.CoverUrl,
        c.IsPublished,
        c.Lessons.Count,
        null, null, null, null
    );
}
