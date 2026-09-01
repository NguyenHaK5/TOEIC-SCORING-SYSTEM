using System;
using System.Collections.Generic;

namespace Toeic.Application.HumanRating.Dtos;

public class AiHumanComparisonFilter
{
    /// <summary>Lọc theo QuestionType (PICTURE_SENTENCE/WRITTEN_RESPONSE/OPINION_ESSAY). Null = tất cả.</summary>
    public string? QuestionType { get; set; }

    /// <summary>
    /// True (mặc định): chỉ trả về các cặp có ĐỦ cả AI (WritingAssessment.IsCurrent) và Human — vì
    /// Phase 5 (MAE/RMSE/Correlation) cần dataset dạng cặp điểm, thiếu 1 bên thì vô nghĩa với thống kê.
    /// False: trả cả các bài chỉ có Human (chưa có AI) hoặc chỉ có AI (chưa Human chấm) — dùng để xem
    /// tiến độ chấm chồng chéo, không dùng để tính metrics.
    /// </summary>
    public bool OnlyWithBothScores { get; set; } = true;

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class CriterionComparisonDto
{
    public string Criterion { get; set; } = string.Empty;
    public decimal? AiScore { get; set; }
    public decimal? HumanScore { get; set; }

    /// <summary>HumanScore - AiScore. Null nếu thiếu 1 trong 2 bên.</summary>
    public decimal? Difference { get; set; }
}

/// <summary>
/// 1 cặp so sánh AI vs Human cho 1 CandidateAnswer + 1 HumanAssessment cụ thể (một câu có thể có
/// nhiều HumanAssessment nếu nhiều Rater cùng chấm — mỗi bản tạo 1 dòng so sánh riêng, phục vụ tính
/// Agreement Rate giữa nhiều Rater ở Phase 5, không chỉ riêng bản IsFinal).
/// </summary>
public class AiHumanComparisonDto
{
    public Guid CandidateAnswerId { get; set; }
    public Guid QuestionId { get; set; }
    public string QuestionType { get; set; } = string.Empty;

    public Guid HumanAssessmentId { get; set; }
    public Guid RaterId { get; set; }
    public string RaterName { get; set; } = string.Empty;
    public bool IsFinalHuman { get; set; }
    public decimal HumanOverallScore { get; set; }

    public Guid? WritingAssessmentId { get; set; }
    public decimal? AiOverallScore { get; set; }

    /// <summary>HumanOverallScore - AiOverallScore. Null nếu chưa có AI assessment.</summary>
    public decimal? OverallDifference { get; set; }

    public List<CriterionComparisonDto> CriterionComparisons { get; set; } = new();
}
