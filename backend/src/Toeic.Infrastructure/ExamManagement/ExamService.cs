using Microsoft.EntityFrameworkCore;
using Toeic.Application.Common.Dtos;
using Toeic.Application.Common.Exceptions;
using Toeic.Application.ExamManagement;
using Toeic.Application.ExamManagement.Dtos;
using Toeic.Domain.Entities;
using Toeic.Domain.Enums;
using Toeic.Infrastructure.Persistence;

namespace Toeic.Infrastructure.ExamManagement;

/// <summary>Implement IExamService dùng thẳng AppDbContext (nhất quán quyết định Part 1/2).</summary>
public class ExamService : IExamService
{
    private readonly AppDbContext _db;

    /// <summary>State machine hợp lệ cho Exam.Status — key = trạng thái hiện tại, value = tập trạng thái được phép chuyển tới.</summary>
    private static readonly Dictionary<ExamStatus, ExamStatus[]> AllowedTransitions = new()
    {
        [ExamStatus.DRAFT] = new[] { ExamStatus.SCHEDULED, ExamStatus.CANCELLED },
        [ExamStatus.SCHEDULED] = new[] { ExamStatus.ONGOING, ExamStatus.CANCELLED, ExamStatus.DRAFT },
        [ExamStatus.ONGOING] = new[] { ExamStatus.COMPLETED, ExamStatus.CANCELLED },
        [ExamStatus.COMPLETED] = Array.Empty<ExamStatus>(),
        [ExamStatus.CANCELLED] = Array.Empty<ExamStatus>(),
    };

    public ExamService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ExamDto> CreateAsync(CreateExamRequest request, Guid createdByUserId, CancellationToken ct = default)
    {
        ValidateTimeRange(request.StartTime, request.EndTime);

        var codeExists = await _db.Exams.AnyAsync(e => e.ExamCode == request.ExamCode, ct);
        if (codeExists)
        {
            throw new ConflictAppException($"ExamCode '{request.ExamCode}' đã tồn tại.");
        }

        var now = DateTime.UtcNow;
        var exam = new Exam
        {
            Id = Guid.NewGuid(),
            ExamCode = request.ExamCode.Trim(),
            ExamName = request.ExamName.Trim(),
            Description = request.Description,
            ExamDate = request.ExamDate,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Status = ExamStatus.DRAFT,
            CreatedByUserId = createdByUserId,
            CreatedAt = now,
        };

        _db.Exams.Add(exam);
        await _db.SaveChangesAsync(ct);

        return ToDto(exam, Array.Empty<ExamTestSummaryDto>());
    }

    public async Task<ExamDto> UpdateAsync(Guid examId, UpdateExamRequest request, CancellationToken ct = default)
    {
        ValidateTimeRange(request.StartTime, request.EndTime);

        var exam = await GetEntityOrThrowAsync(examId, ct);

        if (exam.Status != ExamStatus.DRAFT)
        {
            throw new ConflictAppException("Chỉ có thể sửa Exam đang ở trạng thái DRAFT.");
        }

        exam.ExamName = request.ExamName.Trim();
        exam.Description = request.Description;
        exam.ExamDate = request.ExamDate;
        exam.StartTime = request.StartTime;
        exam.EndTime = request.EndTime;
        exam.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        var assigned = await GetAssignedTestsAsync(examId, ct);
        return ToDto(exam, assigned);
    }

    public async Task<ExamDto> GetByIdAsync(Guid examId, CancellationToken ct = default)
    {
        var exam = await GetEntityOrThrowAsync(examId, ct);
        var assigned = await GetAssignedTestsAsync(examId, ct);
        return ToDto(exam, assigned);
    }

    public async Task<PagedResult<ExamListItemDto>> ListAsync(ExamListQuery query, CancellationToken ct = default)
    {
        var q = _db.Exams.AsNoTracking().AsQueryable();

        if (query.Status is not null)
        {
            q = q.Where(e => e.Status == query.Status);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            q = q.Where(e => EF.Functions.ILike(e.ExamName, $"%{term}%") || EF.Functions.ILike(e.ExamCode, $"%{term}%"));
        }

        var totalCount = await q.CountAsync(ct);

        var items = await q
            .OrderByDescending(e => e.ExamDate)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(e => new ExamListItemDto
            {
                ExamId = e.Id,
                ExamCode = e.ExamCode,
                ExamName = e.ExamName,
                ExamDate = e.ExamDate,
                Status = e.Status,
                AssignedTestCount = e.ExamTests.Count(et => et.IsActive),
            })
            .ToListAsync(ct);

        return new PagedResult<ExamListItemDto>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount,
        };
    }

    public async Task DeleteAsync(Guid examId, CancellationToken ct = default)
    {
        var exam = await GetEntityOrThrowAsync(examId, ct);

        if (exam.Status != ExamStatus.DRAFT)
        {
            throw new ConflictAppException("Chỉ có thể xoá Exam đang ở trạng thái DRAFT.");
        }

        var hasSessions = await _db.ExamSessions.AnyAsync(s => s.ExamId == examId, ct);
        if (hasSessions)
        {
            throw new ConflictAppException("Không thể xoá Exam đã có ExamSession (thí sinh đã tham gia).");
        }

        _db.Exams.Remove(exam); // Cascade xoá ExamTests liên quan (không xoá Test gốc).
        await _db.SaveChangesAsync(ct);
    }

    public async Task<ExamDto> ChangeStatusAsync(Guid examId, ExamStatus newStatus, CancellationToken ct = default)
    {
        var exam = await GetEntityOrThrowAsync(examId, ct);

        if (!AllowedTransitions.TryGetValue(exam.Status, out var allowed) || !allowed.Contains(newStatus))
        {
            throw new ConflictAppException($"Không thể chuyển Exam từ {exam.Status} sang {newStatus}.");
        }

        if (newStatus == ExamStatus.SCHEDULED)
        {
            var hasActiveTest = await _db.ExamTests.AnyAsync(et => et.ExamId == examId && et.IsActive, ct);
            if (!hasActiveTest)
            {
                throw new ValidationAppException("Exam phải có ít nhất 1 Test đang được gán (IsActive) trước khi chuyển sang SCHEDULED.");
            }
        }

        exam.Status = newStatus;
        exam.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        var assigned = await GetAssignedTestsAsync(examId, ct);
        return ToDto(exam, assigned);
    }

    public async Task<ExamTestSummaryDto> AssignTestAsync(Guid examId, Guid testId, CancellationToken ct = default)
    {
        var exam = await GetEntityOrThrowAsync(examId, ct);
        EnsureExamContentIsEditable(exam);

        var test = await _db.Tests.FirstOrDefaultAsync(t => t.Id == testId, ct)
            ?? throw new NotFoundAppException("Không tìm thấy Test.");

        if (test.Status != TestStatus.PUBLISHED)
        {
            throw new ValidationAppException($"Chỉ có thể gán Test đang PUBLISHED vào Exam (hiện tại: {test.Status}).");
        }

        var existing = await _db.ExamTests.FirstOrDefaultAsync(et => et.ExamId == examId && et.TestId == testId, ct);
        if (existing is not null)
        {
            existing.IsActive = true; // Idempotent: gán lại 1 Test đã từng gỡ trước đó.
        }
        else
        {
            _db.ExamTests.Add(new ExamTest
            {
                ExamId = examId,
                TestId = testId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            });
        }

        await _db.SaveChangesAsync(ct);

        return new ExamTestSummaryDto
        {
            TestId = test.Id,
            TestCode = test.TestCode,
            TestName = test.TestName,
            IsActive = true,
        };
    }

    public async Task UnassignTestAsync(Guid examId, Guid testId, CancellationToken ct = default)
    {
        var exam = await GetEntityOrThrowAsync(examId, ct);
        EnsureExamContentIsEditable(exam);

        var examTest = await _db.ExamTests.FirstOrDefaultAsync(et => et.ExamId == examId && et.TestId == testId, ct)
            ?? throw new NotFoundAppException("Test này chưa được gán vào Exam.");

        // KHÔNG xoá dòng — chỉ tắt IsActive, giữ lịch sử vì ExamSessions cũ vẫn tham chiếu
        // (ExamId, TestId) này (ISSUE-01, Phase 0).
        examTest.IsActive = false;
        await _db.SaveChangesAsync(ct);
    }

    // --- Helpers ------------------------------------------------------------

    private async Task<Exam> GetEntityOrThrowAsync(Guid examId, CancellationToken ct)
        => await _db.Exams.FirstOrDefaultAsync(e => e.Id == examId, ct)
           ?? throw new NotFoundAppException("Không tìm thấy Exam.");

    private static void EnsureExamContentIsEditable(Exam exam)
    {
        if (exam.Status != ExamStatus.DRAFT && exam.Status != ExamStatus.SCHEDULED)
        {
            throw new ConflictAppException("Chỉ có thể gán/gỡ Test khi Exam đang DRAFT hoặc SCHEDULED (chưa ONGOING).");
        }
    }

    private static void ValidateTimeRange(DateTime start, DateTime end)
    {
        if (end <= start)
        {
            throw new ValidationAppException("EndTime phải sau StartTime.", new Dictionary<string, string[]>
            {
                ["EndTime"] = new[] { "EndTime phải sau StartTime." }
            });
        }
    }

    private async Task<List<ExamTestSummaryDto>> GetAssignedTestsAsync(Guid examId, CancellationToken ct)
        => await _db.ExamTests
            .Where(et => et.ExamId == examId)
            .Include(et => et.Test)
            .Select(et => new ExamTestSummaryDto
            {
                TestId = et.TestId,
                TestCode = et.Test.TestCode,
                TestName = et.Test.TestName,
                IsActive = et.IsActive,
            })
            .ToListAsync(ct);

    private static ExamDto ToDto(Exam e, IReadOnlyList<ExamTestSummaryDto> assignedTests) => new()
    {
        ExamId = e.Id,
        ExamCode = e.ExamCode,
        ExamName = e.ExamName,
        Description = e.Description,
        ExamDate = e.ExamDate,
        StartTime = e.StartTime,
        EndTime = e.EndTime,
        Status = e.Status,
        CreatedByUserId = e.CreatedByUserId,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt,
        AssignedTests = assignedTests,
    };
}
