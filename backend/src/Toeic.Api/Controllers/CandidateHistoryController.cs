using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Toeic.Application.Common.Dtos;
using Toeic.Application.Dashboard;
using Toeic.Application.Dashboard.Dtos;

namespace Toeic.Api.Controllers;

/// <summary>
/// Phase 6 Part 1 — History module.
/// ⚠️ Kế thừa từ ApiControllerBase (Phase 1 Part 3) như các controller khác — nếu
/// ApiControllerBase đã có sẵn helper lấy UserId hiện tại (vd. GetCurrentUserId()),
/// hãy DÙNG LẠI helper đó thay vì đoạn đọc claim thủ công dưới đây, để nhất quán với
/// ExamSessionsController (Phase 1 Part 4). Mình không có source thật của
/// ApiControllerBase/ExamSessionsController nên viết tường minh tại đây — đây là điểm
/// Dragon cần đối chiếu và có thể rút gọn.
/// </summary>
[ApiController]
[Route("api/v1/candidates/me")]
[Authorize(Roles = "CANDIDATE")]
public sealed class CandidateHistoryController : ApiControllerBase
{
    private readonly IHistoryService _historyService;

    public CandidateHistoryController(IHistoryService historyService)
    {
        _historyService = historyService;
    }

    /// <summary>
    /// GET /api/v1/candidates/me/history?page=1&amp;pageSize=10&amp;status=SUBMITTED
    /// </summary>
    [HttpGet("history")]
    public async Task<ActionResult<PagedResult<ExamHistoryItemDto>>> GetHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();

        var result = await _historyService.GetCandidateHistoryAsync(
            currentUserId,
            new HistoryQueryParams { Page = page, PageSize = pageSize, Status = status },
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Đọc UserId từ claim "sub" (chuẩn JWT, xem README Phase 1 Part 2: claim gồm
    /// sub/role/email/username/jti). Fallback ClaimTypes.NameIdentifier vì một số cấu
    /// hình JwtBearer tự map "sub" -> NameIdentifier.
    /// </summary>
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
