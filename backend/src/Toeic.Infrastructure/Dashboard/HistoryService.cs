using Microsoft.EntityFrameworkCore;
using Toeic.Application.Common.Dtos;
using Toeic.Application.Common.Exceptions;
using Toeic.Application.Dashboard;
using Toeic.Application.Dashboard.Dtos;
using Toeic.Domain.Enums;
using Toeic.Infrastructure.Persistence;

namespace Toeic.Infrastructure.Dashboard;

public sealed class HistoryService : IHistoryService
{
    private readonly AppDbContext _db;
    private readonly IWritingSessionScoreAggregator _writingAggregator;

    public HistoryService(AppDbContext db, IWritingSessionScoreAggregator writingAggregator)
    {
        _db = db;
        _writingAggregator = writingAggregator;
    }

    public async Task<PagedResult<ExamHistoryItemDto>> GetCandidateHistoryAsync(
        Guid currentUserId,
        HistoryQueryParams query,
        CancellationToken cancellationToken)
    {
        // Resolve UserId (JWT "sub") -> CandidateId, đúng quan hệ 1:1 Users<->Candidates
        // đã chuẩn hoá ở Phase 0 (ISSUE-08).
        var candidateId = await _db.Candidates
            .Where(c => c.UserId == currentUserId)
            .Select(c => c.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (candidateId == Guid.Empty)
        {
            throw new NotFoundAppException("Không tìm thấy hồ sơ Candidate cho tài khoản hiện tại.");
        }

        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize is < 1 or > 100 ? 10 : query.PageSize;

        var sessionsQuery = _db.ExamSessions
            .Where(s => s.CandidateId == candidateId);

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (!Enum.TryParse<ExamSessionStatus>(query.Status, ignoreCase: true, out var statusEnum))
            {
                throw new ValidationAppException($"Status không hợp lệ: '{query.Status}'.");
            }

            sessionsQuery = sessionsQuery.Where(s => s.Status == statusEnum);
        }
        else
        {
            // Mặc định chỉ hiển thị các phiên đã nộp bài — IN_PROGRESS/CANCELLED không
            // phải là "lịch sử" theo nghĩa candidate cần xem lại kết quả.
            sessionsQuery = sessionsQuery.Where(s => s.Status == ExamSessionStatus.SUBMITTED);
        }

        var totalCount = await sessionsQuery.CountAsync(cancellationToken);

        var pageItems = await sessionsQuery
            .OrderByDescending(s => s.StartedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new
            {
                s.Id,
                s.ExamId,
                s.Exam.ExamCode,
                s.Exam.ExamName,
                s.Test.TestCode,
                s.StartedAt,
                s.SubmittedAt,
                s.Status,
                s.ListeningScore,
                s.ReadingScore
            })
            .ToListAsync(cancellationToken);

        var sessionIds = pageItems.Select(i => i.Id).ToList();

        // 2 lời gọi gộp (không phải N+1) cho toàn bộ trang hiện tại — xem PHASE6-ISSUE-01.
        var writingScores = await _writingAggregator.GetWritingScoresForSessionsAsync(sessionIds, cancellationToken);
        var writingCounts = await _writingAggregator.GetWritingQuestionCountsAsync(sessionIds, cancellationToken);

        var items = pageItems.Select(i =>
        {
            var hasWriting = writingCounts.TryGetValue(i.Id, out var wCount) && wCount > 0;
            var wScore = writingScores.TryGetValue(i.Id, out var sc) ? sc : null;

            var writingStatus = !hasWriting
                ? "NO_WRITING_QUESTIONS"
                : wScore is null
                    ? "PENDING"
                    : "SCORED";

            return new ExamHistoryItemDto
            {
                ExamSessionId = i.Id,
                ExamId = i.ExamId,
                ExamCode = i.ExamCode,
                ExamName = i.ExamName,
                TestCode = i.TestCode,
                StartedAt = i.StartedAt,
                SubmittedAt = i.SubmittedAt,
                Status = i.Status.ToString(),
                ListeningScore = i.ListeningScore,
                ReadingScore = i.ReadingScore,
                WritingScore = wScore,
                WritingStatus = writingStatus
            };
        }).ToList();

        return new PagedResult<ExamHistoryItemDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}
