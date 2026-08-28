using Toeic.Application.ExamTaking.Dtos;

namespace Toeic.Application.ExamTaking;

public interface IExamSessionService
{
    /// <summary>
    /// Tạo phiên thi mới cho Candidate (xác định qua UserId lấy từ JWT). Nếu Candidate đã có
    /// phiên NOT_STARTED/IN_PROGRESS cho cùng Exam, trả lại phiên đó (resume, idempotent) thay vì
    /// tạo mới (ISSUE-06, Phase 0). TestId được hệ thống tự chọn ngẫu nhiên trong số các Test đang
    /// IsActive của Exam (hỗ trợ nhiều form FORM-A/FORM-B, ISSUE-01). ExpiresAt chốt một lần tại
    /// đây = thời điểm bắt đầu + tổng thời lượng 3 phần của Test (ISSUE-07).
    /// </summary>
    Task<ExamSessionDto> StartAsync(Guid candidateUserId, StartExamSessionRequest request, CancellationToken ct = default);

    /// <summary>Trả về phiên thi — chỉ chủ sở hữu (Candidate) mới xem được. Tự động auto-submit nếu đã hết hạn.</summary>
    Task<ExamSessionDto> GetByIdAsync(Guid sessionId, Guid requestingUserId, CancellationToken ct = default);

    /// <summary>Danh sách câu hỏi của Test thuộc phiên thi, ẩn Answer Key, kèm câu trả lời đã lưu (nếu có).</summary>
    Task<IReadOnlyList<ExamSessionQuestionDto>> GetQuestionsAsync(Guid sessionId, Guid requestingUserId, CancellationToken ct = default);

    /// <summary>
    /// Auto-save 1 câu trả lời — UPSERT theo UNIQUE(ExamSessionId, QuestionId) (ISSUE-05). Ném
    /// ConflictAppException nếu phiên đã hết giờ/không còn IN_PROGRESS.
    /// </summary>
    Task<CandidateAnswerDto> SaveAnswerAsync(Guid sessionId, Guid requestingUserId, SaveAnswerRequest request, CancellationToken ct = default);

    /// <summary>Nộp bài — idempotent (gọi lại khi đã SUBMITTED vẫn trả về thành công, không lỗi).</summary>
    Task<ExamSessionDto> SubmitAsync(Guid sessionId, Guid requestingUserId, CancellationToken ct = default);
}
