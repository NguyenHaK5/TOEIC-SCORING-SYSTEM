using Microsoft.EntityFrameworkCore;
using Toeic.Application.Common.Dtos;
using Toeic.Application.Common.Exceptions;
using Toeic.Application.ExamManagement;
using Toeic.Application.ExamManagement.Dtos;
using Toeic.Domain.Entities;
using Toeic.Domain.Enums;
using Toeic.Infrastructure.Persistence;

namespace Toeic.Infrastructure.ExamManagement;

/// <summary>Implement ITestService dùng thẳng AppDbContext (nhất quán quyết định Part 1/2 — không Repository&lt;T&gt;).</summary>
public class TestService : ITestService
{
    private readonly AppDbContext _db;

    public TestService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<TestDto> CreateAsync(CreateTestRequest request, Guid createdByUserId, CancellationToken ct = default)
    {
        var codeExists = await _db.Tests.AnyAsync(t => t.TestCode == request.TestCode, ct);
        if (codeExists)
        {
            throw new ConflictAppException($"TestCode '{request.TestCode}' đã tồn tại.");
        }

        var now = DateTime.UtcNow;
        var test = new Test
        {
            Id = Guid.NewGuid(),
            TestCode = request.TestCode.Trim(),
            TestName = request.TestName.Trim(),
            Version = request.Version.Trim(),
            Description = request.Description,
            ListeningDurationSeconds = request.ListeningDurationSeconds,
            ReadingDurationSeconds = request.ReadingDurationSeconds,
            WritingDurationSeconds = request.WritingDurationSeconds,
            Status = TestStatus.DRAFT,
            CreatedByUserId = createdByUserId,
            CreatedAt = now,
        };

        _db.Tests.Add(test);
        await _db.SaveChangesAsync(ct);

        return ToDto(test, questionCount: 0, listening: 0, reading: 0, writing: 0);
    }

    public async Task<TestDto> UpdateAsync(Guid testId, UpdateTestRequest request, CancellationToken ct = default)
    {
        var test = await GetEntityOrThrowAsync(testId, ct);

        if (test.Status != TestStatus.DRAFT)
        {
            throw new ConflictAppException("Chỉ có thể sửa Test đang ở trạng thái DRAFT — Test đã PUBLISHED phải giữ nguyên nội dung để bảo đảm tính toàn vẹn cho các phiên thi đang/đã diễn ra.");
        }

        test.TestName = request.TestName.Trim();
        test.Version = request.Version.Trim();
        test.Description = request.Description;
        test.ListeningDurationSeconds = request.ListeningDurationSeconds;
        test.ReadingDurationSeconds = request.ReadingDurationSeconds;
        test.WritingDurationSeconds = request.WritingDurationSeconds;
        test.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        var counts = await GetQuestionCountsAsync(testId, ct);
        return ToDto(test, counts.total, counts.listening, counts.reading, counts.writing);
    }

    public async Task<TestDto> GetByIdAsync(Guid testId, CancellationToken ct = default)
    {
        var test = await GetEntityOrThrowAsync(testId, ct);
        var counts = await GetQuestionCountsAsync(testId, ct);
        return ToDto(test, counts.total, counts.listening, counts.reading, counts.writing);
    }

    public async Task<PagedResult<TestListItemDto>> ListAsync(TestListQuery query, CancellationToken ct = default)
    {
        var q = _db.Tests.AsNoTracking().AsQueryable();

        if (query.Status is not null)
        {
            q = q.Where(t => t.Status == query.Status);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            q = q.Where(t => EF.Functions.ILike(t.TestName, $"%{term}%") || EF.Functions.ILike(t.TestCode, $"%{term}%"));
        }

        var totalCount = await q.CountAsync(ct);

        var items = await q
            .OrderByDescending(t => t.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(t => new TestListItemDto
            {
                TestId = t.Id,
                TestCode = t.TestCode,
                TestName = t.TestName,
                Version = t.Version,
                Status = t.Status,
                QuestionCount = t.Questions.Count,
            })
            .ToListAsync(ct);

        return new PagedResult<TestListItemDto>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount,
        };
    }

    public async Task DeleteAsync(Guid testId, CancellationToken ct = default)
    {
        var test = await GetEntityOrThrowAsync(testId, ct);

        if (test.Status != TestStatus.DRAFT)
        {
            throw new ConflictAppException("Chỉ có thể xoá Test đang ở trạng thái DRAFT.");
        }

        var assignedToAnyExam = await _db.ExamTests.AnyAsync(et => et.TestId == testId, ct);
        if (assignedToAnyExam)
        {
            throw new ConflictAppException("Không thể xoá Test đã được gán vào ít nhất 1 Exam. Hãy gỡ gán trước.");
        }

        _db.Tests.Remove(test); // Cascade xoá Questions/QuestionOptions của chính Test này (chưa từng dùng để thi).
        await _db.SaveChangesAsync(ct);
    }

    public async Task<TestDto> PublishAsync(Guid testId, CancellationToken ct = default)
    {
        var test = await GetEntityOrThrowAsync(testId, ct);

        if (test.Status != TestStatus.DRAFT)
        {
            throw new ConflictAppException($"Chỉ có thể publish Test đang DRAFT (hiện tại: {test.Status}).");
        }

        var questions = await _db.Questions
            .Include(q => q.Options)
            .Where(q => q.TestId == testId)
            .ToListAsync(ct);

        var errors = ValidateQuestionsForPublish(questions);
        if (errors.Count > 0)
        {
            throw new ValidationAppException(
                "Không thể publish Test vì còn câu hỏi chưa hợp lệ.",
                errors.ToDictionary(e => e.Key, e => e.Value));
        }

        test.Status = TestStatus.PUBLISHED;
        test.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        var counts = await GetQuestionCountsAsync(testId, ct);
        return ToDto(test, counts.total, counts.listening, counts.reading, counts.writing);
    }

    public async Task<TestDto> ArchiveAsync(Guid testId, CancellationToken ct = default)
    {
        var test = await GetEntityOrThrowAsync(testId, ct);

        if (test.Status != TestStatus.PUBLISHED)
        {
            throw new ConflictAppException($"Chỉ có thể archive Test đang PUBLISHED (hiện tại: {test.Status}).");
        }

        test.Status = TestStatus.ARCHIVED;
        test.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        var counts = await GetQuestionCountsAsync(testId, ct);
        return ToDto(test, counts.total, counts.listening, counts.reading, counts.writing);
    }

    // --- Helpers ------------------------------------------------------------

    private async Task<Test> GetEntityOrThrowAsync(Guid testId, CancellationToken ct)
        => await _db.Tests.FirstOrDefaultAsync(t => t.Id == testId, ct)
           ?? throw new NotFoundAppException("Không tìm thấy Test.");

    private async Task<(int total, int listening, int reading, int writing)> GetQuestionCountsAsync(Guid testId, CancellationToken ct)
    {
        var counts = await _db.Questions
            .Where(q => q.TestId == testId)
            .GroupBy(q => q.Section)
            .Select(g => new { Section = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        int Get(Section s) => counts.FirstOrDefault(c => c.Section == s)?.Count ?? 0;
        var total = counts.Sum(c => c.Count);
        return (total, Get(Section.LISTENING), Get(Section.READING), Get(Section.WRITING));
    }

    /// <summary>
    /// Business rule validation trước khi publish (mục "AI có validation AI output" và
    /// "Section=LISTENING cần AudioUrl" — Phase 0, enforce ở Application layer, KHÔNG CHECK constraint).
    /// </summary>
    private static Dictionary<string, string[]> ValidateQuestionsForPublish(List<Question> questions)
    {
        var errors = new Dictionary<string, List<string>>();

        void AddError(Question q, string message)
        {
            var key = $"Question[{q.Section}#{q.QuestionNumber}]";
            if (!errors.TryGetValue(key, out var list))
            {
                list = new List<string>();
                errors[key] = list;
            }
            list.Add(message);
        }

        if (questions.Count == 0)
        {
            errors["Test"] = new List<string> { "Test chưa có câu hỏi nào." };
            return errors.ToDictionary(kv => kv.Key, kv => kv.Value.ToArray());
        }

        foreach (var q in questions)
        {
            if (q.Section == Section.LISTENING && string.IsNullOrWhiteSpace(q.AudioUrl))
            {
                AddError(q, "Câu hỏi Listening bắt buộc phải có AudioUrl.");
            }

            if (q.QuestionType == QuestionType.PICTURE_SENTENCE && string.IsNullOrWhiteSpace(q.ImageUrl))
            {
                AddError(q, "Câu hỏi Picture Description (Q1–5) bắt buộc phải có ImageUrl.");
            }

            if (q.QuestionType == QuestionType.MULTIPLE_CHOICE)
            {
                if (q.Options.Count < 2)
                {
                    AddError(q, "Câu trắc nghiệm phải có tối thiểu 2 phương án.");
                }

                var correctCount = q.Options.Count(o => o.IsCorrect);
                if (correctCount != 1)
                {
                    AddError(q, $"Câu trắc nghiệm phải có đúng 1 đáp án đúng (hiện tại: {correctCount}).");
                }
            }
            else if (q.Options.Count > 0)
            {
                AddError(q, "Câu hỏi Writing không được có QuestionOptions (chỉ MULTIPLE_CHOICE mới có).");
            }
        }

        return errors.ToDictionary(kv => kv.Key, kv => kv.Value.ToArray());
    }

    private static TestDto ToDto(Test t, int questionCount, int listening, int reading, int writing) => new()
    {
        TestId = t.Id,
        TestCode = t.TestCode,
        TestName = t.TestName,
        Version = t.Version,
        Description = t.Description,
        ListeningDurationSeconds = t.ListeningDurationSeconds,
        ReadingDurationSeconds = t.ReadingDurationSeconds,
        WritingDurationSeconds = t.WritingDurationSeconds,
        Status = t.Status,
        CreatedByUserId = t.CreatedByUserId,
        CreatedAt = t.CreatedAt,
        UpdatedAt = t.UpdatedAt,
        QuestionCount = questionCount,
        ListeningQuestionCount = listening,
        ReadingQuestionCount = reading,
        WritingQuestionCount = writing,
    };
}
