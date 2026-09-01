using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Toeic.Application.Dashboard;

/// <summary>
/// PHASE6-ISSUE-01: batch-resolve điểm Writing cho NHIỀU ExamSession cùng lúc,
/// tránh N+1 khi gọi IWritingScoreResolver (Phase 4 Part 2) từng CandidateAnswer một
/// cho toàn bộ danh sách lịch sử.
///
/// Áp dụng ĐÚNG rule ưu tiên đã chốt ở Phase 0/Phase 4 (ISSUE-04):
///   điểm câu Writing = HumanAssessment.IsFinal nếu có, else WritingAssessment.IsCurrent.
/// Điểm phiên = trung bình cộng các câu Writing ĐÃ CÓ điểm (câu chưa chấm bị loại khỏi
/// trung bình, không tính là 0).
///
/// ⚠️ Công thức "trung bình cộng các câu Writing trong 1 phiên" ở đây là GIẢ ĐỊNH của
/// Claude — Dragon cần đối chiếu với công thức thật đang dùng trong
/// ExamSessionService.GetResultAsync (Phase 3 Part 4) để đảm bảo History và Result
/// hiển thị cùng 1 con số cho cùng 1 phiên thi. Nếu công thức thật khác (vd. có trọng số
/// theo Points của câu hỏi), sửa lại WritingSessionScoreAggregator cho khớp — đây là điểm
/// rủi ro cao nhất của Part 1 này.
/// </summary>
public interface IWritingSessionScoreAggregator
{
    /// <summary>
    /// Trả về dictionary ExamSessionId -> điểm Writing trung bình (null nếu phiên không có
    /// câu Writing nào đã chấm xong).
    /// </summary>
    Task<IReadOnlyDictionary<Guid, decimal?>> GetWritingScoresForSessionsAsync(
        IReadOnlyCollection<Guid> examSessionIds,
        CancellationToken cancellationToken);

    /// <summary>
    /// Trả về dictionary ExamSessionId -> số câu Writing trong phiên đó (kể cả chưa chấm).
    /// Dùng để phân biệt "không có câu Writing" (đề không có Writing) với "có câu Writing
    /// nhưng đang PENDING" — 2 trạng thái này phải hiển thị khác nhau cho Candidate.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, int>> GetWritingQuestionCountsAsync(
        IReadOnlyCollection<Guid> examSessionIds,
        CancellationToken cancellationToken);
}
