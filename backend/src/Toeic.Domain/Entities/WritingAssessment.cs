using Toeic.Domain.Common;

namespace Toeic.Domain.Entities;

/// <summary>
/// Kết quả AI chấm 1 câu Writing. Quan hệ 1:N với CandidateAnswer (ISSUE-02) — mỗi lần
/// chấm lại (rubric đổi version) tạo 1 dòng MỚI, KHÔNG update dòng cũ, để giữ audit trail.
/// IsCurrent đánh dấu bản hiện hành; partial unique index UNIQUE(CandidateAnswerId) WHERE IsCurrent = TRUE
/// đảm bảo bảo luôn có đúng 1 bản hiện hành.
/// Các cột điểm theo tiêu chí cứng (Grammar/Vocabulary/...) đã được BỎ khỏi bảng này (ISSUE-03) và
/// chuyển sang AssessmentCriterionScores để hỗ trợ tiêu chí linh hoạt theo Rubric (vd Image Relevance Q1-5)
/// và so sánh AI vs Human theo từng tiêu chí ở module Evaluation.
/// </summary>
public class WritingAssessment : BaseEntity
{
    public Guid CandidateAnswerId { get; set; }
    public Guid RubricSetId { get; set; }

    public decimal? OverallScore { get; set; }

    /// <summary>Danh sách lỗi phát hiện (JSON) — dạng cấu trúc theo Structured AI Output.</summary>
    public string? ErrorsJson { get; set; }

    /// <summary>Toàn bộ kết quả phân tích chi tiết từ AI (JSON), phục vụ audit.</summary>
    public string? AnalysisJson { get; set; }

    public string? Feedback { get; set; }
    public string? ModelName { get; set; }
    public string? PromptVersion { get; set; }
    public bool IsCurrent { get; set; } = true;

    // Navigation
    public CandidateAnswer CandidateAnswer { get; set; } = default!;
    public RubricSet RubricSet { get; set; } = default!;
    public ICollection<AssessmentCriterionScore> CriterionScores { get; set; } = new List<AssessmentCriterionScore>();
}
