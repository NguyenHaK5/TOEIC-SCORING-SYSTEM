using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Toeic.Application.Dashboard;
using Toeic.Application.Dashboard.Dtos;

namespace Toeic.Api.Controllers;

/// <summary>
/// Phase 6 Part 3 — Admin Dashboard tổng quan + Writing Error Analytics.
/// Khác với CandidateHistoryController/CandidateAnalyticsController (Part 1/2, phạm vi
/// 1 Candidate), controller này yêu cầu Role=ADMIN và gộp trên TOÀN HỆ THỐNG.
/// </summary>
[ApiController]
[Route("api/v1/admin/dashboard")]
[Authorize(Roles = "ADMIN")]
public sealed class AdminDashboardController : ApiControllerBase
{
    private readonly IAdminDashboardService _dashboardService;

    public AdminDashboardController(IAdminDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    /// <summary>GET /api/v1/admin/dashboard/overview</summary>
    [HttpGet("overview")]
    public async Task<ActionResult<AdminDashboardOverviewDto>> GetOverview(
        CancellationToken cancellationToken)
    {
        var result = await _dashboardService.GetOverviewAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>GET /api/v1/admin/dashboard/writing-errors?topN=20</summary>
    [HttpGet("writing-errors")]
    public async Task<ActionResult<WritingErrorAnalyticsResponseDto>> GetWritingErrors(
        [FromQuery] int topN = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _dashboardService.GetWritingErrorAnalyticsAsync(topN, cancellationToken);
        return Ok(result);
    }
}
