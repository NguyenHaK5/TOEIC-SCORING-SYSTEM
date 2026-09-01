using System;
using System.Collections.Generic;

namespace Toeic.Application.HumanRating.Dtos;

/// <summary>
/// Bộ lọc cho danh sách bài Writing trong Rater Dashboard.
/// </summary>
public enum RaterQueueStatusFilter
{
    /// <summary>Chưa có HumanAssessment nào với IsFinal = true.</summary>
    Pending,
    /// <summary>Đã có HumanAssessment với IsFinal = true.</summary>
    Rated,
    All
}

public class HumanRatingQueueFilter
{
    public RaterQueueStatusFilter Status { get; set; } = RaterQueueStatusFilter.Pending;

    /// <summary>QUESTION_TYPE lọc theo Q1-5/Q6-7/Q8 (PICTURE_SENTENCE/WRITTEN_RESPONSE/OPINION_ESSAY). Null = tất cả.</summary>
    public string? QuestionType { get; set; }

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>1 dòng trong danh sách Rater Dashboard.</summary>
public class WritingAnswerQueueItemDto
{
    public Guid CandidateAnswerId { get; set; }
    public Guid ExamSessionId { get; set; }
    public Guid CandidateId { get; set; }
    public string CandidateName { get; set; } = string.Empty;
    public Guid QuestionId { get; set; }
    public string QuestionType { get; set; } = string.Empty;
    public int QuestionNumber { get; set; }
    public DateTime SubmittedAt { get; set; }

    /// <summary>"Scored" | "Pending" | "NotFound" — trạng thái AI assessment (Phase 3 Part 4).</summary>
    public string AiStatus { get; set; } = "NotFound";
    public decimal? AiOverallScore { get; set; }

    /// <summary>"NotRated" | "Rated" — có HumanAssessment IsFinal=true chưa.</summary>
    public string HumanStatus { get; set; } = "NotRated";
    public decimal? HumanFinalScore { get; set; }
}

public class CriterionScoreDto
{
    public string Criterion { get; set; } = string.Empty;
    public decimal Score { get; set; }
    public decimal? Weight { get; set; }
    public string? Reasoning { get; set; }
}

public class AiAssessmentSummaryDto
{
    public Guid WritingAssessmentId { get; set; }
    public string? ModelName { get; set; }
    public string? PromptVersion { get; set; }
    public decimal OverallScore { get; set; }
    public string? Feedback { get; set; }
    public List<string> Strengths { get; set; } = new();
    public List<string> Weaknesses { get; set; } = new();
    public List<CriterionScoreDto> CriterionScores { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class HumanAssessmentSummaryDto
{
    public Guid HumanAssessmentId { get; set; }
    public Guid RaterId { get; set; }
    public string RaterName { get; set; } = string.Empty;
    public decimal OverallScore { get; set; }
    public string? Feedback { get; set; }
    public bool IsFinal { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<CriterionScoreDto> CriterionScores { get; set; } = new();
}

public class RubricCriterionDto
{
    public Guid RubricId { get; set; }
    public string Criterion { get; set; } = string.Empty;
    public int Level { get; set; }
    public decimal MinScore { get; set; }
    public decimal MaxScore { get; set; }
    public string? Description { get; set; }
    public decimal Weight { get; set; }
}

public class ActiveRubricSetDto
{
    public Guid RubricSetId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public List<RubricCriterionDto> Criteria { get; set; } = new();
}

/// <summary>Chi tiết 1 bài Writing để Rater chấm — câu trả lời, AI assessment, rubric, lịch sử human đã chấm.</summary>
public class WritingAnswerDetailDto
{
    public Guid CandidateAnswerId { get; set; }
    public Guid ExamSessionId { get; set; }
    public Guid CandidateId { get; set; }
    public string CandidateName { get; set; } = string.Empty;

    public Guid QuestionId { get; set; }
    public string QuestionType { get; set; } = string.Empty;
    public string? QuestionText { get; set; }
    public string? RequiredWords { get; set; }
    public string? ImageUrl { get; set; }

    public string? AnswerText { get; set; }

    public AiAssessmentSummaryDto? AiAssessment { get; set; }
    public List<HumanAssessmentSummaryDto> HumanAssessments { get; set; } = new();

    /// <summary>Rubric ACTIVE hiện hành cho QuestionType này — Rater PHẢI chấm đúng theo tập Criterion này.</summary>
    public ActiveRubricSetDto? ActiveRubric { get; set; }
}

public class CriterionScoreInputDto
{
    public string Criterion { get; set; } = string.Empty;
    public decimal Score { get; set; }
    public string? Reasoning { get; set; }
}

public class SubmitHumanScoreRequestDto
{
    public List<CriterionScoreInputDto> CriterionScores { get; set; } = new();
    public string? Feedback { get; set; }

    /// <summary>
    /// True (mặc định): kết quả này trở thành điểm CHÍNH THỨC (IsFinal=true), các HumanAssessment
    /// IsFinal cũ (nếu có, từ Rater khác chấm trước) bị chuyển về false. False: chỉ lưu để tham khảo/
    /// đối chiếu (ví dụ Rater thứ 2 chấm cùng bài để tính Agreement Rate ở Phase 5), không thay đổi
    /// điểm chính thức hiện tại.
    /// </summary>
    public bool SetAsFinal { get; set; } = true;
}

public class HumanScoreResultDto
{
    public Guid HumanAssessmentId { get; set; }
    public Guid CandidateAnswerId { get; set; }
    public decimal OverallScore { get; set; }
    public bool IsFinal { get; set; }
    public List<CriterionScoreDto> CriterionScores { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}
