using System;
using System.Collections.Generic;

namespace Toeic.Application.Dashboard.Dtos;

/// <summary>
/// 1 nhóm thống kê đúng/sai cho Listening/Reading, nhóm theo (Section, QuestionType).
/// ⚠️ PHASE6-ISSUE-05: với schema hiện tại, QuestionType chỉ có MULTIPLE_CHOICE cho cả
/// Listening và Reading -> kết quả thực tế sẽ tương đương nhóm theo Section. Sẵn sàng
/// chi tiết hơn (vd. theo TOEIC Part 1-7) nếu Dragon mở rộng schema sau này.
/// </summary>
public sealed class ObjectiveWeaknessItemDto
{
    public string Section { get; set; } = default!;       // LISTENING | READING
    public string QuestionType { get; set; } = default!;  // hiện tại luôn MULTIPLE_CHOICE
    public int CorrectCount { get; set; }
    public int TotalCount { get; set; }
    public decimal AccuracyRate { get; set; }              // 0.0 - 1.0
    public int SampleSize { get; set; }                    // = TotalCount, để Frontend cảnh báo cỡ mẫu nhỏ
}

/// <summary>
/// 1 tiêu chí Writing (Grammar, Vocabulary, Image Relevance, ...) và điểm trung bình
/// của Candidate trên tiêu chí đó, gộp qua nhiều bài Writing đã có điểm chính thức.
/// Xem PHASE6-ISSUE-04 về cách chọn đúng bản chấm "chính thức" trước khi gộp.
/// </summary>
public sealed class WritingCriterionWeaknessItemDto
{
    public string CriterionCode { get; set; } = default!;
    public decimal AverageScore { get; set; }
    public int SampleSize { get; set; }                    // số câu Writing đã có điểm chính thức đóng góp vào trung bình này
}

/// <summary>1 điểm trên biểu đồ xu hướng điểm theo thời gian.</summary>
public sealed class ScoreTrendPointDto
{
    public Guid ExamSessionId { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public decimal? ListeningScore { get; set; }
    public decimal? ReadingScore { get; set; }
    public decimal? WritingScore { get; set; }             // null nếu chưa chấm xong (PENDING) hoặc đề không có Writing
}

/// <summary>Response gộp cho GET /candidates/me/analytics.</summary>
public sealed class CandidateAnalyticsResponseDto
{
    /// <summary>Sắp xếp yếu nhất (AccuracyRate thấp nhất) lên đầu.</summary>
    public List<ObjectiveWeaknessItemDto> ObjectiveWeakness { get; set; } = new();

    /// <summary>Sắp xếp yếu nhất (AverageScore thấp nhất) lên đầu.</summary>
    public List<WritingCriterionWeaknessItemDto> WritingWeakness { get; set; } = new();

    /// <summary>Sắp xếp theo thời gian tăng dần (cũ -> mới), phù hợp vẽ line chart.</summary>
    public List<ScoreTrendPointDto> ScoreTrend { get; set; } = new();
}

public sealed class AnalyticsQueryParams
{
    /// <summary>Giới hạn số phiên gần nhất dùng cho ScoreTrend (tránh trả về quá nhiều điểm khi lịch sử dài).</summary>
    public int TrendLimit { get; set; } = 20;
}
