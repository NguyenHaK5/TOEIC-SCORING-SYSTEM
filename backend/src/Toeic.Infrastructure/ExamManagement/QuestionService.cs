using Microsoft.EntityFrameworkCore;
using Toeic.Application.Common.Exceptions;
using Toeic.Application.ExamManagement;
using Toeic.Application.ExamManagement.Dtos;
using Toeic.Domain.Entities;
using Toeic.Domain.Enums;
using Toeic.Infrastructure.Persistence;

namespace Toeic.Infrastructure.ExamManagement;

/// <summary>
/// Implement IQuestionService dùng thẳng AppDbContext. Quy tắc quan trọng: Question/QuestionOption
/// CHỈ được thêm/sửa/xoá khi Test cha đang ở trạng thái DRAFT — bảo vệ tính toàn vẹn nội dung đề
/// sau khi đã PUBLISHED (thí sinh có thể đã/đang làm bài dựa trên nội dung đó).
/// </summary>
public class QuestionService : IQuestionService
{
    private readonly AppDbContext _db;

    public QuestionService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<QuestionDto> CreateAsync(Guid testId, CreateQuestionRequest request, Guid createdByUserId, CancellationToken ct = default)
    {
        var test = await GetDraftTestOrThrowAsync(testId, ct);

        ValidateQuestionShape(request.Section, request.QuestionType, request.AudioUrl, request.ImageUrl);

        var duplicateNumber = await _db.Questions.AnyAsync(
            q => q.TestId == testId && q.Section == request.Section && q.QuestionNumber == request.QuestionNumber, ct);
        if (duplicateNumber)
        {
            throw new ConflictAppException($"Đã tồn tại câu hỏi số {request.QuestionNumber} trong phần {request.Section} của Test này.");
        }

        var now = DateTime.UtcNow;
        var question = new Question
        {
            Id = Guid.NewGuid(),
            TestId = test.Id,
            Section = request.Section,
            QuestionType = request.QuestionType,
            QuestionNumber = request.QuestionNumber,
            QuestionText = request.QuestionText,
            ImageUrl = request.ImageUrl,
            AudioUrl = request.AudioUrl,
            RequiredWords = request.RequiredWords,
            Points = request.Points,
            CreatedByUserId = createdByUserId,
            CreatedAt = now,
        };

        _db.Questions.Add(question);
        await _db.SaveChangesAsync(ct);

        return ToDto(question, Array.Empty<QuestionOption>());
    }

    public async Task<QuestionDto> UpdateAsync(Guid questionId, UpdateQuestionRequest request, CancellationToken ct = default)
    {
        var question = await _db.Questions
            .Include(q => q.Test)
            .Include(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == questionId, ct)
            ?? throw new NotFoundAppException("Không tìm thấy câu hỏi.");

        EnsureTestIsDraft(question.Test);

        var duplicateNumber = await _db.Questions.AnyAsync(
            q => q.TestId == question.TestId && q.Section == question.Section
                 && q.QuestionNumber == request.QuestionNumber && q.Id != questionId, ct);
        if (duplicateNumber)
        {
            throw new ConflictAppException($"Đã tồn tại câu hỏi số {request.QuestionNumber} trong phần {question.Section} của Test này.");
        }

        question.QuestionNumber = request.QuestionNumber;
        question.QuestionText = request.QuestionText;
        question.ImageUrl = request.ImageUrl;
        question.AudioUrl = request.AudioUrl;
        question.RequiredWords = request.RequiredWords;
        question.Points = request.Points;
        question.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return ToDto(question, question.Options);
    }

    public async Task<QuestionDto> GetByIdAsync(Guid questionId, CancellationToken ct = default)
    {
        var question = await _db.Questions
            .Include(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == questionId, ct)
            ?? throw new NotFoundAppException("Không tìm thấy câu hỏi.");

        return ToDto(question, question.Options);
    }

    public async Task<IReadOnlyList<QuestionDto>> ListByTestAsync(Guid testId, Section? section, CancellationToken ct = default)
    {
        var testExists = await _db.Tests.AnyAsync(t => t.Id == testId, ct);
        if (!testExists)
        {
            throw new NotFoundAppException("Không tìm thấy Test.");
        }

        var q = _db.Questions.Include(x => x.Options).Where(x => x.TestId == testId);
        if (section is not null)
        {
            q = q.Where(x => x.Section == section);
        }

        var questions = await q.OrderBy(x => x.Section).ThenBy(x => x.QuestionNumber).ToListAsync(ct);
        return questions.Select(x => ToDto(x, x.Options)).ToList();
    }

    public async Task DeleteAsync(Guid questionId, CancellationToken ct = default)
    {
        var question = await _db.Questions
            .Include(q => q.Test)
            .FirstOrDefaultAsync(q => q.Id == questionId, ct)
            ?? throw new NotFoundAppException("Không tìm thấy câu hỏi.");

        EnsureTestIsDraft(question.Test);

        _db.Questions.Remove(question); // Cascade xoá QuestionOptions liên quan.
        await _db.SaveChangesAsync(ct);
    }

    public async Task<QuestionOptionDto> AddOptionAsync(Guid questionId, CreateQuestionOptionRequest request, CancellationToken ct = default)
    {
        var question = await _db.Questions
            .Include(q => q.Test)
            .Include(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == questionId, ct)
            ?? throw new NotFoundAppException("Không tìm thấy câu hỏi.");

        EnsureTestIsDraft(question.Test);

        if (question.QuestionType != QuestionType.MULTIPLE_CHOICE)
        {
            throw new ValidationAppException("Chỉ câu hỏi MULTIPLE_CHOICE mới có QuestionOptions.");
        }

        var labelExists = question.Options.Any(o => o.OptionLabel == request.OptionLabel);
        if (labelExists)
        {
            throw new ConflictAppException($"Nhãn phương án '{request.OptionLabel}' đã tồn tại trong câu hỏi này.");
        }

        var option = new QuestionOption
        {
            Id = Guid.NewGuid(),
            QuestionId = question.Id,
            OptionLabel = request.OptionLabel,
            OptionText = request.OptionText,
            IsCorrect = request.IsCorrect,
            CreatedAt = DateTime.UtcNow,
        };

        _db.QuestionOptions.Add(option);
        await _db.SaveChangesAsync(ct);

        return ToOptionDto(option);
    }

    public async Task<QuestionOptionDto> UpdateOptionAsync(Guid optionId, UpdateQuestionOptionRequest request, CancellationToken ct = default)
    {
        var option = await _db.QuestionOptions
            .Include(o => o.Question).ThenInclude(q => q.Test)
            .FirstOrDefaultAsync(o => o.Id == optionId, ct)
            ?? throw new NotFoundAppException("Không tìm thấy phương án.");

        EnsureTestIsDraft(option.Question.Test);

        var labelConflict = await _db.QuestionOptions.AnyAsync(
            o => o.QuestionId == option.QuestionId && o.OptionLabel == request.OptionLabel && o.Id != optionId, ct);
        if (labelConflict)
        {
            throw new ConflictAppException($"Nhãn phương án '{request.OptionLabel}' đã tồn tại trong câu hỏi này.");
        }

        option.OptionLabel = request.OptionLabel;
        option.OptionText = request.OptionText;
        option.IsCorrect = request.IsCorrect;

        await _db.SaveChangesAsync(ct);

        return ToOptionDto(option);
    }

    public async Task DeleteOptionAsync(Guid optionId, CancellationToken ct = default)
    {
        var option = await _db.QuestionOptions
            .Include(o => o.Question).ThenInclude(q => q.Test)
            .FirstOrDefaultAsync(o => o.Id == optionId, ct)
            ?? throw new NotFoundAppException("Không tìm thấy phương án.");

        EnsureTestIsDraft(option.Question.Test);

        _db.QuestionOptions.Remove(option);
        await _db.SaveChangesAsync(ct);
    }

    // --- Helpers ------------------------------------------------------------

    private async Task<Test> GetDraftTestOrThrowAsync(Guid testId, CancellationToken ct)
    {
        var test = await _db.Tests.FirstOrDefaultAsync(t => t.Id == testId, ct)
            ?? throw new NotFoundAppException("Không tìm thấy Test.");
        EnsureTestIsDraft(test);
        return test;
    }

    private static void EnsureTestIsDraft(Test test)
    {
        if (test.Status != TestStatus.DRAFT)
        {
            throw new ConflictAppException(
                "Chỉ có thể thêm/sửa/xoá câu hỏi khi Test đang ở trạng thái DRAFT — Test đã PUBLISHED phải giữ nguyên nội dung.");
        }
    }

    /// <summary>Validation hình dạng câu hỏi theo Section/QuestionType — enforce ở Application layer (Phase 0), không CHECK constraint.</summary>
    private static void ValidateQuestionShape(Section section, QuestionType type, string? audioUrl, string? imageUrl)
    {
        var errors = new List<string>();

        var sectionMatchesType = (section, type) switch
        {
            (Section.LISTENING, QuestionType.MULTIPLE_CHOICE) => true,
            (Section.READING, QuestionType.MULTIPLE_CHOICE) => true,
            (Section.WRITING, QuestionType.PICTURE_SENTENCE) => true,
            (Section.WRITING, QuestionType.WRITTEN_RESPONSE) => true,
            (Section.WRITING, QuestionType.OPINION_ESSAY) => true,
            _ => false,
        };
        if (!sectionMatchesType)
        {
            errors.Add($"Section={section} không tương thích với QuestionType={type}.");
        }

        if (section == Section.LISTENING && string.IsNullOrWhiteSpace(audioUrl))
        {
            errors.Add("Câu hỏi Listening bắt buộc phải có AudioUrl (có thể cập nhật sau, nhưng khuyến nghị nhập ngay).");
        }

        if (errors.Count > 0)
        {
            throw new ValidationAppException("Dữ liệu câu hỏi không hợp lệ.", new Dictionary<string, string[]>
            {
                ["Question"] = errors.ToArray()
            });
        }
    }

    private static QuestionDto ToDto(Question q, IEnumerable<QuestionOption> options) => new()
    {
        QuestionId = q.Id,
        TestId = q.TestId,
        Section = q.Section,
        QuestionType = q.QuestionType,
        QuestionNumber = q.QuestionNumber,
        QuestionText = q.QuestionText,
        ImageUrl = q.ImageUrl,
        AudioUrl = q.AudioUrl,
        RequiredWords = q.RequiredWords,
        Points = q.Points,
        Options = options.OrderBy(o => o.OptionLabel).Select(ToOptionDto).ToList(),
    };

    private static QuestionOptionDto ToOptionDto(QuestionOption o) => new()
    {
        QuestionOptionId = o.Id,
        QuestionId = o.QuestionId,
        OptionLabel = o.OptionLabel,
        OptionText = o.OptionText,
        IsCorrect = o.IsCorrect,
    };
}
