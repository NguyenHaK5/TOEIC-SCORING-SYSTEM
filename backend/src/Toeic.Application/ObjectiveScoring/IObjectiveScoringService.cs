using System;
using System.Threading;
using System.Threading.Tasks;
using Toeic.Application.ObjectiveScoring.Dtos;

namespace Toeic.Application.ObjectiveScoring;

/// <summary>
/// Objective Scoring Engine — chấm tự động Listening/Reading (mục 3.1 Project.docx, Phase 2 roadmap).
/// Ranh giới module: chỉ xử lý câu hỏi MULTIPLE_CHOICE thuộc Section LISTENING/READING.
/// Không đụng tới Writing (thuộc AI Assessment — Phase 3).
/// </summary>
public interface IObjectiveScoringService
{
    /// <summary>
    /// Chấm (hoặc chấm lại) toàn bộ câu hỏi Listening/Reading của một ExamSession.
    /// Yêu cầu: session phải đang ở trạng thái SUBMITTED (đã nộp bài) — ném
    /// <c>ConflictAppException</c> nếu chưa.
    /// <para>
    /// Idempotent: có thể gọi lại nhiều lần an toàn — mỗi lần chấm lại sẽ TÍNH LẠI TOÀN BỘ
    /// từ Answer Key hiện tại, không cộng dồn điểm cũ. Điều này cũng có nghĩa: nếu Answer Key
    /// bị Admin sửa sau khi thí sinh đã thi (trường hợp hiếm, ngoài luồng bình thường vì Test
    /// đã PUBLISHED thì bị khoá — xem Phase 1 Part 3), gọi lại hàm này sẽ chấm theo Answer Key
    /// MỚI NHẤT, không phải Answer Key tại thời điểm nộp bài. Đây là đánh đổi có chủ đích để
    /// giữ logic đơn giản (KISS) — Writing Assessment mới cần audit trail đầy đủ theo version
    /// (rubric), Objective Scoring không có yêu cầu "audit vì sao được điểm đó" tương tự vì đáp
    /// án đúng/sai là sự thật khách quan, không phải đánh giá chủ quan.
    /// </para>
    /// </summary>
    Task<ObjectiveScoringSummaryDto> ScoreSessionAsync(Guid examSessionId, CancellationToken ct = default);

    /// <summary>
    /// Lấy kết quả Objective Scoring của một phiên thi cho chính thí sinh sở hữu phiên đó.
    /// Nếu phiên thi chưa từng được chấm (ví dụ dữ liệu cũ trước khi Phase 2 triển khai, hoặc
    /// lần trigger chấm tự động ở Submit bị bỏ lỡ), hàm này sẽ tự chấm ngay tại thời điểm gọi
    /// (lazy scoring) trước khi trả kết quả — cùng triết lý "lazy auto-submit" đã áp dụng ở
    /// Phase 1 Part 4.
    /// </summary>
    /// <param name="requestingUserId">UserId của người gọi — dùng để kiểm tra quyền sở hữu phiên thi.</param>
    Task<ExamSessionResultDto> GetResultAsync(Guid examSessionId, Guid requestingUserId, CancellationToken ct = default);
}
