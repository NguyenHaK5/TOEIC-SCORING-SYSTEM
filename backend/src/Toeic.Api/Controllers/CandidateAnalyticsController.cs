using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Toeic.Application.Dashboard;
using Toeic.Application.Dashboard.Dtos;

namespace Toeic.Api.Controllers;

/// <summary>
/// Phase 6 Part 2 — Candidate Performance Analytics.
/// ⚠️ Cùng lưu ý như CandidateHistoryController (Part 1): nếu ApiControllerBase/
/// ExamSessionsController thật đã có sẵn helper lấy UserId, dùng lại helper đó thay vì
/// đọc claim thủ công ở đây.
/// </summary>
[ApiController]
[Route("api/v1/candidates/me")]
[Authorize(Roles = "CANDIDATE")]
public sealed class CandidateAnalyticsController : ApiControllerBase
{
    private readonly ICandidateAnalyticsService _analyticsService;

    public CandidateAnalyticsController(ICandidateAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    /// <summary>
    /// GET /api/v1/candidates/me/analytics?trendLimit=20
    /// Trả về Strength/Weakness (Listening/Reading theo QuestionType, Writing theo
    /// Criterion) và xu hướng điểm theo thời gian, gộp trên toàn bộ phiên thi đã nộp.
    /// </summary>
    [HttpGet("analytics")]
    public async Task<ActionResult<CandidateAnalyticsResponseDto>> GetAnalytics(
        [FromQuery] int trendLimit = 20,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();

        var result = await _analyticsService.GetAnalyticsAsync(
            currentUserId,
            new AnalyticsQueryParams { TrendLimit = trendLimit },
            cancellationToken);

        return Ok(result);
    }

    private Guid GetCurrentUserId()
    {
        var raw = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(raw) || !Guid.TryParse(raw, out var userId))
        {
            throw new UnauthorizedAccessException("Không xác định được UserId từ token.");
        }

        return userId;
    }
}
