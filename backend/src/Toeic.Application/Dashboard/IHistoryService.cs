using System;
using System.Threading;
using System.Threading.Tasks;
using Toeic.Application.Common.Dtos;
using Toeic.Application.Dashboard.Dtos;

namespace Toeic.Application.Dashboard;

/// <summary>
/// Phase 6 Part 1 — History module.
/// Chỉ đọc dữ liệu (không ghi), nên không cần transaction/concurrency handling.
/// </summary>
public interface IHistoryService
{
    /// <summary>
    /// Trả về lịch sử các phiên thi của Candidate gắn với tài khoản <paramref name="currentUserId"/>
    /// (UserId lấy từ JWT claim "sub", KHÔNG phải CandidateId — service tự resolve
    /// qua Candidates.UserId, giống cách ExamSessionService đang làm ở Phase 1 Part 4).
    /// </summary>
    Task<PagedResult<ExamHistoryItemDto>> GetCandidateHistoryAsync(
        Guid currentUserId,
        HistoryQueryParams query,
        CancellationToken cancellationToken);
}
