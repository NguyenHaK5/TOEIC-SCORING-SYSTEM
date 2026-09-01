using System.Threading;
using System.Threading.Tasks;
using Toeic.Application.Dashboard.Dtos;

namespace Toeic.Application.Dashboard;

/// <summary>
/// Phase 6 Part 3 — Admin Dashboard tổng quan + Writing Error Analytics.
/// Chỉ đọc dữ liệu, gộp trên TOÀN HỆ THỐNG (không riêng 1 Candidate) — khác với
/// ICandidateAnalyticsService (Part 2, phạm vi 1 Candidate).
/// </summary>
public interface IAdminDashboardService
{
    Task<AdminDashboardOverviewDto> GetOverviewAsync(CancellationToken cancellationToken);

    /// <param name="topN">Số nhóm lỗi phổ biến nhất trả về (mặc định 20, tối đa 100).</param>
    Task<WritingErrorAnalyticsResponseDto> GetWritingErrorAnalyticsAsync(
        int topN,
        CancellationToken cancellationToken);
}
