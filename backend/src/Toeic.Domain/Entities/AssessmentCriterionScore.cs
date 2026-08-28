using Toeic.Domain.Enums;

namespace Toeic.Domain.Entities;

/// <summary>
/// Điểm theo từng tiêu chí Rubric, dùng chung cho cả AI và Human (ISSUE-03 — bảng MỚI).
/// Đúng 1 trong 2 khóa ngoại (WritingAssessmentId / HumanAssessmentId) được set, tương ứng
/// AssessmentType. Nhờ dùng chung RubricId, AI và Human luôn so sánh cùng 1 tập tiêu chí,
/// kể cả Image Relevance của Q1–5 — module Evaluation truy vấn bằng 1 JOIN đơn giản.
/// CHECK constraint: WritingAssessmentId IS NOT NULL OR HumanAssessmentId IS NOT NULL.
/// </summary>
public class AssessmentCriterionScore
{
    public Guid Id { get; set; }
    public AssessmentSourceType AssessmentType { get; set; }

    /// <summary>Set khi AssessmentType = AI.</summary>
    public Guid? WritingAssessmentId { get; set; }

    /// <summary>Set khi AssessmentType = HUMAN.</summary>
    public Guid? HumanAssessmentId { get; set; }

    public Guid RubricId { get; set; }

    /// <summary>Nhãn tiêu chí tại thời điểm chấm — denormalized để chống rubric đổi tên sau này làm sai lệch lịch sử.</summary>
    public string CriterionCode { get; set; } = default!;

    public decimal Score { get; set; }
    public string? Reasoning { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation
    public WritingAssessment? WritingAssessment { get; set; }
    public HumanAssessment? HumanAssessment { get; set; }
    public Rubric Rubric { get; set; } = default!;
}
