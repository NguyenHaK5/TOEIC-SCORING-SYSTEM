using System.Collections.Generic;

namespace Toeic.Application.Dashboard.Dtos;

/// <summary>
/// Thống kê tóm tắt cho 1 loại điểm (Listening/Reading/Writing) trên toàn hệ thống.
/// PHASE6-ISSUE-07: dùng summary stats thay vì histogram cố định vì chưa có TOEIC scaled
/// score (5-495) — raw max điểm khác nhau tuỳ Test nên histogram theo khoảng cố định sẽ
/// vô nghĩa khi gộp nhiều Test.
/// </summary>
public sealed class ScoreSummaryDto
{
    public int SampleSize { get; set; }
    public decimal? Average { get; set; }
    public decimal? Min { get; set; }
    public decimal? Max { get; set; }
    public decimal? Median { get; set; }
}

/// <summary>Tổng quan hệ thống cho Admin Dashboard.</summary>
public sealed class AdminDashboardOverviewDto
{
    public int TotalCandidates { get; set; }
    public int TotalExams { get; set; }
    public int TotalExamSessions { get; set; }
    public int SubmittedSessionsCount { get; set; }
    public int InProgressSessionsCount { get; set; }

    public ScoreSummaryDto ListeningScoreSummary { get; set; } = new();
    public ScoreSummaryDto ReadingScoreSummary { get; set; } = new();
    public ScoreSummaryDto WritingScoreSummary { get; set; } = new();
}

/// <summary>
/// 1 nhóm lỗi Writing phổ biến, đếm theo (QuestionType, Criterion, ErrorType).
/// ⚠️ PHASE6-ISSUE-06: Criterion/ErrorType có thể là "UNSPECIFIED"/"UNKNOWN" nếu
/// ErrorsJson thật không khớp field name giả định — xem integration guide.
/// </summary>
public sealed class WritingErrorAnalyticsItemDto
{
    public string QuestionType { get; set; } = default!; // PICTURE_SENTENCE | WRITTEN_RESPONSE | OPINION_ESSAY
    public string Criterion { get; set; } = default!;
    public string ErrorType { get; set; } = default!;
    public int Count { get; set; }
}

public sealed class WritingErrorAnalyticsResponseDto
{
    /// <summary>Sắp xếp giảm dần theo Count (lỗi phổ biến nhất lên đầu).</summary>
    public List<WritingErrorAnalyticsItemDto> TopErrors { get; set; } = new();

    /// <summary>Tổng số WritingAssessment (IsCurrent=true, thuộc phiên SUBMITTED) đã đưa vào phân tích.</summary>
    public int TotalAssessmentsAnalyzed { get; set; }

    /// <summary>
    /// Số bản ghi có ErrorsJson KHÔNG parse được (JSON không hợp lệ) — minh bạch cho Admin
    /// biết có bao nhiêu dữ liệu bị bỏ qua, không âm thầm loại bỏ (PHASE6-ISSUE-06).
    /// </summary>
    public int UnparseableErrorsJsonCount { get; set; }
}
