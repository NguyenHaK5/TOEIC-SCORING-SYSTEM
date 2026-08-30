using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Toeic.Application.ExamTaking;
using Toeic.Application.ExamTaking.Dtos;
using Toeic.Application.ObjectiveScoring;
using Toeic.Domain.Enums;

namespace Toeic.Api.Controllers;

/// <summary>
/// "Phòng thi trực tuyến" (mục 16, prompt gốc) — toàn bộ endpoint chỉ CANDIDATE được gọi và chỉ
/// thao tác được trên phiên thi của chính mình (ownership check trong Service). Server luôn là
/// nguồn xác thực thời gian cuối cùng — Frontend không được tự tính giờ hết hạn.
/// </summary>
[ApiController]
[Route("api/v1/exam-sessions")]
[Authorize(Roles = nameof(UserRole.CANDIDATE))]
public class ExamSessionsController : ApiControllerBase
{
    private readonly IExamSessionService _examSessionService;
    private readonly IObjectiveScoringService _objectiveScoringService;

    public ExamSessionsController(IExamSessionService examSessionService, IObjectiveScoringService objectiveScoringService)
    {
        _examSessionService = examSessionService;
        _objectiveScoringService = objectiveScoringService;
    }

    /// <summary>Bắt đầu (hoặc resume nếu đã có phiên đang diễn ra) 1 phiên thi cho Exam đang ONGOING.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ExamSessionDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Start([FromBody] StartExamSessionRequest request, CancellationToken ct)
    {
        var session = await _examSessionService.StartAsync(CurrentUserId, request, ct);
        return CreatedAtAction(nameof(GetById), new { sessionId = session.ExamSessionId }, session);
    }

    [HttpGet("{sessionId:guid}")]
    [ProducesResponseType(typeof(ExamSessionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid sessionId, CancellationToken ct)
    {
        var session = await _examSessionService.GetByIdAsync(sessionId, CurrentUserId, ct);
        return Ok(session);
    }

    /// <summary>Danh sách câu hỏi của Test thuộc phiên thi — KHÔNG bao gồm Answer Key.</summary>
    [HttpGet("{sessionId:guid}/questions")]
    [ProducesResponseType(typeof(IReadOnlyList<ExamSessionQuestionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetQuestions(Guid sessionId, CancellationToken ct)
    {
        var questions = await _examSessionService.GetQuestionsAsync(sessionId, CurrentUserId, ct);
        return Ok(questions);
    }

    /// <summary>Auto-save 1 câu trả lời — gọi lại nhiều lần cho cùng QuestionId sẽ UPSERT, không tạo trùng.</summary>
    [HttpPost("{sessionId:guid}/answers")]
    [ProducesResponseType(typeof(CandidateAnswerDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> SaveAnswer(Guid sessionId, [FromBody] SaveAnswerRequest request, CancellationToken ct)
    {
        var answer = await _examSessionService.SaveAnswerAsync(sessionId, CurrentUserId, request, ct);
        return Ok(answer);
    }

    /// <summary>Nộp bài — idempotent, gọi lại khi đã nộp vẫn trả 200 (không lỗi).</summary>
    [HttpPost("{sessionId:guid}/submit")]
    [ProducesResponseType(typeof(ExamSessionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Submit(Guid sessionId, CancellationToken ct)
    {
        var session = await _examSessionService.SubmitAsync(sessionId, CurrentUserId, ct);
        return Ok(session);
    }

    /// <summary>Lấy kết quả điểm số Listening/Reading sau khi nộp bài.</summary>
    [HttpGet("{sessionId:guid}/result")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetResult(Guid sessionId, CancellationToken ct)
    {
        // Dùng CurrentUserId có sẵn từ ApiControllerBase
        var result = await _objectiveScoringService.GetResultAsync(sessionId, CurrentUserId, ct);
        return Ok(result);
    }
}
