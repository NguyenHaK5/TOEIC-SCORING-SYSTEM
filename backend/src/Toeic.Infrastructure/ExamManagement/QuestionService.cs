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




    // File: backend/src/Toeic.Infrastructure/ExamManagement/QuestionService.cs — thêm method
    public async Task<BulkCreateQuestionsResultDto> BulkCreateAsync(
        Guid testId, BulkCreateQuestionsRequestDto request, CancellationToken ct = default)
    {
        var test = await _db.Tests.FirstOrDefaultAsync(t => t.Id == testId, ct)
            ?? throw new NotFoundAppException("Test không tồn tại.");

        if (test.Status != TestStatus.DRAFT)
            throw new ConflictAppException("Chỉ được thêm câu hỏi hàng loạt khi Test đang ở trạng thái DRAFT.");

        var errors = new List<BulkRowErrorDto>();
        var validRows = new List<(BulkQuestionRowDto Row, Question Entity)>();

        // 1) Validate TOÀN BỘ trước, không dừng ở dòng đầu tiên (đúng triết lý Test.Publish())
        var seenQuestionNumbers = new HashSet<int>();
        foreach (var row in request.Rows)
        {
            var rowErrors = new List<string>();

            if (!Enum.TryParse<Section>(row.Section, ignoreCase: true, out var section))
                rowErrors.Add($"Section không hợp lệ: '{row.Section}'.");
            if (!Enum.TryParse<QuestionType>(row.QuestionType, ignoreCase: true, out var qType))
                rowErrors.Add($"QuestionType không hợp lệ: '{row.QuestionType}'.");
            if (string.IsNullOrWhiteSpace(row.QuestionText))
                rowErrors.Add("Thiếu QuestionText.");
            if (!seenQuestionNumbers.Add(row.QuestionNumber))
                rowErrors.Add($"QuestionNumber {row.QuestionNumber} bị trùng trong file.");

            if (rowErrors.Count == 0)
            {
                // Tái dùng đúng rule đã có ở Test.Publish() / QuestionService.CreateAsync hiện có:
                if (section == Section.LISTENING && string.IsNullOrWhiteSpace(row.AudioUrl))
                    rowErrors.Add("Listening bắt buộc có AudioUrl.");
                if (row.QuestionNumber is >= 1 and <= 5 && string.IsNullOrWhiteSpace(row.ImageUrl))
                    rowErrors.Add("Picture Description (Q1-5) bắt buộc có ImageUrl.");
                if (qType == QuestionType.MULTIPLE_CHOICE)
                {
                    if (row.Options.Count < 2)
                        rowErrors.Add("MULTIPLE_CHOICE cần tối thiểu 2 option.");
                    if (row.Options.Count(o => o.IsCorrect) != 1)
                        rowErrors.Add("MULTIPLE_CHOICE cần đúng 1 option IsCorrect = true.");
                }
            }

            if (rowErrors.Count > 0)
            {
                errors.Add(new BulkRowErrorDto(row.RowNumber, rowErrors));
                continue;
            }

            var entity = new Question
            {
                Id = Guid.NewGuid(),
                TestId = testId,
                Section = Enum.Parse<Section>(row.Section, true),
                QuestionType = Enum.Parse<QuestionType>(row.QuestionType, true),
                QuestionNumber = row.QuestionNumber,
                QuestionText = row.QuestionText,
                ImageUrl = row.ImageUrl,
                AudioUrl = row.AudioUrl,
                RequiredWords = row.RequiredWords,
                Points = row.Points,
                CreatedAt = DateTime.UtcNow,
                Options = row.Options.Select(o => new QuestionOption
                {
                    Id = Guid.NewGuid(),
                    OptionLabel = o.OptionLabel,
                    OptionText = o.OptionText,
                    IsCorrect = o.IsCorrect,
                    CreatedAt = DateTime.UtcNow
                }).ToList()
            };
            validRows.Add((row, entity));
        }

        // 2) All-or-nothing: nếu có bất kỳ dòng lỗi, KHÔNG ghi gì cả, trả về danh sách lỗi đầy đủ
        if (errors.Count > 0)
        {
            return new BulkCreateQuestionsResultDto(
                Success: false, TotalRows: request.Rows.Count, CreatedCount: 0, Errors: errors);
        }

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        _db.Questions.AddRange(validRows.Select(v => v.Entity));
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return new BulkCreateQuestionsResultDto(
            Success: true, TotalRows: request.Rows.Count, CreatedCount: validRows.Count, Errors: []);
    }




    // --- Refactor: tách phần validate dùng chung cho cả Bulk Create (ISSUE-19) và Bulk Update ---
    private List<string> ValidateRowCommon(
        string section, string questionType, string questionText,
        int questionNumber, string? imageUrl, string? audioUrl,
        List<BulkQuestionOptionDto> options)
    {
        var errors = new List<string>();

        if (!Enum.TryParse<Section>(section, ignoreCase: true, out var parsedSection))
            errors.Add($"Section không hợp lệ: '{section}'.");
        if (!Enum.TryParse<QuestionType>(questionType, ignoreCase: true, out var parsedType))
            errors.Add($"QuestionType không hợp lệ: '{questionType}'.");
        if (string.IsNullOrWhiteSpace(questionText))
            errors.Add("Thiếu QuestionText.");

        if (errors.Count == 0)
        {
            if (parsedSection == Section.LISTENING && string.IsNullOrWhiteSpace(audioUrl))
                errors.Add("Listening bắt buộc có AudioUrl.");
            if (questionNumber is >= 1 and <= 5 && string.IsNullOrWhiteSpace(imageUrl))
                errors.Add("Picture Description (Q1-5) bắt buộc có ImageUrl.");
            if (parsedType == QuestionType.MULTIPLE_CHOICE)
            {
                if (options.Count < 2)
                    errors.Add("MULTIPLE_CHOICE cần tối thiểu 2 option.");
                if (options.Count(o => o.IsCorrect) != 1)
                    errors.Add("MULTIPLE_CHOICE cần đúng 1 option IsCorrect = true.");
            }
        }

        return errors;
    }

    public async Task<List<QuestionExportRowDto>> ExportForTestAsync(Guid testId, CancellationToken ct = default)
    {
        var test = await _db.Tests.FirstOrDefaultAsync(t => t.Id == testId, ct)
            ?? throw new NotFoundAppException("Test không tồn tại.");

        return await _db.Questions
            .Where(q => q.TestId == testId)
            .Include(q => q.Options)
            .OrderBy(q => q.QuestionNumber)
            .Select(q => new QuestionExportRowDto(
                q.Id, q.QuestionNumber, q.Section.ToString(), q.QuestionType.ToString(),
                q.QuestionText, q.ImageUrl, q.AudioUrl, q.RequiredWords, q.Points,
                q.Options.Select(o => new BulkQuestionOptionDto(o.OptionLabel, o.OptionText, o.IsCorrect)).ToList()
            ))
            .ToListAsync(ct);
    }

    public async Task<BulkUpdateQuestionsResultDto> BulkUpdateAsync(
        Guid testId, BulkUpdateQuestionsRequestDto request, CancellationToken ct = default)
    {
        var test = await _db.Tests.FirstOrDefaultAsync(t => t.Id == testId, ct)
            ?? throw new NotFoundAppException("Test không tồn tại.");

        if (test.Status != TestStatus.DRAFT)
            throw new ConflictAppException("Chỉ được sửa câu hỏi hàng loạt khi Test đang ở trạng thái DRAFT.");

        // Load toàn bộ câu hỏi hiện có của Test (kèm Options) để: (1) xác nhận QuestionId thuộc đúng Test,
        // (2) kiểm tra trùng QuestionNumber với các câu KHÔNG nằm trong đợt sửa này.
        var existing = await _db.Questions
            .Where(q => q.TestId == testId)
            .Include(q => q.Options)
            .ToDictionaryAsync(q => q.Id, ct);

        var errors = new List<BulkRowErrorDto>();
        var updates = new List<(Question Entity, BulkUpdateQuestionRowDto Row)>();
        var submittedIds = new HashSet<Guid>();
        var submittedNumbers = new HashSet<int>();

        foreach (var row in request.Rows)
        {
            var rowErrors = new List<string>();

            if (!existing.TryGetValue(row.QuestionId, out var entity))
            {
                rowErrors.Add($"QuestionId '{row.QuestionId}' không tồn tại trong Test này.");
            }
            if (!submittedIds.Add(row.QuestionId))
            {
                rowErrors.Add($"QuestionId '{row.QuestionId}' bị lặp lại trong file.");
            }
            if (!submittedNumbers.Add(row.QuestionNumber))
            {
                rowErrors.Add($"QuestionNumber {row.QuestionNumber} bị trùng trong file.");
            }
            else if (existing.Values.Any(q => q.Id != row.QuestionId && q.QuestionNumber == row.QuestionNumber))
            {
                rowErrors.Add($"QuestionNumber {row.QuestionNumber} đã dùng bởi câu hỏi khác không nằm trong đợt sửa này.");
            }

            rowErrors.AddRange(ValidateRowCommon(
                row.Section, row.QuestionType, row.QuestionText, row.QuestionNumber, row.ImageUrl, row.AudioUrl, row.Options));

            if (rowErrors.Count > 0)
            {
                errors.Add(new BulkRowErrorDto(row.RowNumber, rowErrors));
                continue;
            }

            updates.Add((existing[row.QuestionId], row));
        }

        // All-or-nothing: có lỗi bất kỳ dòng nào -> không sửa gì cả
        if (errors.Count > 0)
        {
            return new BulkUpdateQuestionsResultDto(
                Success: false, TotalRows: request.Rows.Count, UpdatedCount: 0, Errors: errors);
        }

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        foreach (var (entity, row) in updates)
        {
            entity.Section = Enum.Parse<Section>(row.Section, true);
            entity.QuestionType = Enum.Parse<QuestionType>(row.QuestionType, true);
            entity.QuestionNumber = row.QuestionNumber;
            entity.QuestionText = row.QuestionText;
            entity.ImageUrl = row.ImageUrl;
            entity.AudioUrl = row.AudioUrl;
            entity.RequiredWords = row.RequiredWords;
            entity.Points = row.Points;
            entity.UpdatedAt = DateTime.UtcNow;

            // Replace toàn bộ Options (đúng Decision ISSUE-20 — an toàn vì Test còn DRAFT,
            // chưa có CandidateAnswer.SelectedOptionId nào tham chiếu QuestionOptionId cũ)
            _db.QuestionOptions.RemoveRange(entity.Options);
            entity.Options = row.Options.Select(o => new QuestionOption
            {
                Id = Guid.NewGuid(),
                QuestionId = entity.Id,
                OptionLabel = o.OptionLabel,
                OptionText = o.OptionText,
                IsCorrect = o.IsCorrect,
                CreatedAt = DateTime.UtcNow
            }).ToList();
        }

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return new BulkUpdateQuestionsResultDto(
            Success: true, TotalRows: request.Rows.Count, UpdatedCount: updates.Count, Errors: []);
    }
}
