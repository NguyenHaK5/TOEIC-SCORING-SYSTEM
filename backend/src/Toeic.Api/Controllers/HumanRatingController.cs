using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Toeic.Application.HumanRating;
using Toeic.Application.HumanRating.Dtos;

namespace Toeic.Api.Controllers;

/// <summary>
/// Phase 4 Part 1 — Rater Dashboard. Toàn bộ endpoint yêu cầu Role = RATER.
/// Base route: /api/v1/rater/writing-answers
/// </summary>
[ApiController]
[Route("api/v1/rater/writing-answers")]
[Authorize(Roles = "RATER")]
public class HumanRatingController : ApiControllerBase
{
    private readonly IHumanRatingService _humanRatingService;

    public HumanRatingController(IHumanRatingService humanRatingService)
    {
        _humanRatingService = humanRatingService;
    }

    /// <summary>GET /api/v1/rater/writing-answers?status=pending&amp;questionType=OPINION_ESSAY&amp;pageNumber=1&amp;pageSize=20</summary>
    [HttpGet]
    public async Task<IActionResult> GetQueue(
        [FromQuery] RaterQueueStatusFilter status = RaterQueueStatusFilter.Pending,
        [FromQuery] string? questionType = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var filter = new HumanRatingQueueFilter
        {
            Status = status,
            QuestionType = questionType,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
        var result = await _humanRatingService.GetQueueAsync(filter, ct);
        return Ok(result);
    }

    /// <summary>GET /api/v1/rater/writing-answers/{candidateAnswerId}</summary>
    [HttpGet("{candidateAnswerId:guid}")]
    public async Task<IActionResult> GetDetail(Guid candidateAnswerId, CancellationToken ct)
    {
        var result = await _humanRatingService.GetDetailAsync(candidateAnswerId, ct);
        return Ok(result);
    }

    /// <summary>GET /api/v1/rater/writing-answers/comparison?questionType=OPINION_ESSAY&amp;onlyWithBothScores=true</summary>
    [HttpGet("comparison")]
    public async Task<IActionResult> GetComparison(
        [FromQuery] string? questionType = null,
        [FromQuery] bool onlyWithBothScores = true,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var filter = new AiHumanComparisonFilter
        {
            QuestionType = questionType,
            OnlyWithBothScores = onlyWithBothScores,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
        var result = await _humanRatingService.GetAiHumanComparisonAsync(filter, ct);
        return Ok(result);
    }

    /// <summary>POST /api/v1/rater/writing-answers/{candidateAnswerId}/score</summary>
    [HttpPost("{candidateAnswerId:guid}/score")]
    public async Task<IActionResult> SubmitScore(
        Guid candidateAnswerId, [FromBody] SubmitHumanScoreRequestDto request, CancellationToken ct)
    {
        var raterId = GetCurrentUserId();
        var result = await _humanRatingService.SubmitScoreAsync(candidateAnswerId, raterId, request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Đọc UserId (RaterId) từ JWT claim "sub" — cùng claim đã dùng ở AuthController/GET /auth/me
    /// (Phase 1 Part 2). Nếu tên claim thật khác "sub", chỉ cần sửa dòng này.
    /// </summary>
    private Guid GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (raw == null || !Guid.TryParse(raw, out var id))
        {
            throw new UnauthorizedAccessException("Không xác định được người dùng hiện tại từ token.");
        }
        return id;
    }
}
