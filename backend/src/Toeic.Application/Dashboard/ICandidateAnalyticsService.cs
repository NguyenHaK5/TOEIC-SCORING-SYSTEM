using System;
using System.Threading;
using System.Threading.Tasks;
using Toeic.Application.Dashboard.Dtos;

namespace Toeic.Application.Dashboard;

/// <summary>
/// Phase 6 Part 2 — Candidate Performance Analytics.
/// Chỉ đọc dữ liệu, gộp từ toàn bộ phiên thi ĐÃ NỘP (SUBMITTED) của Candidate.
/// </summary>
public interface ICandidateAnalyticsService
{
    Task<CandidateAnalyticsResponseDto> GetAnalyticsAsync(
        Guid currentUserId,
        AnalyticsQueryParams query,
        CancellationToken cancellationToken);
}
