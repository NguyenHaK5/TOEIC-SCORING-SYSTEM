using System.ComponentModel.DataAnnotations;
using Toeic.Domain.Enums;

namespace Toeic.Application.ExamManagement.Dtos;

public class CreateQuestionRequest
{
    [Required]
    public Section Section { get; set; }

    [Required]
    public QuestionType QuestionType { get; set; }

    [Range(1, int.MaxValue)]
    public int QuestionNumber { get; set; }

    [Required]
    public string QuestionText { get; set; } = default!;

    public string? ImageUrl { get; set; }
    public string? AudioUrl { get; set; }
    public string? RequiredWords { get; set; }
    public decimal? Points { get; set; }
}

public class UpdateQuestionRequest
{
    [Range(1, int.MaxValue)]
    public int QuestionNumber { get; set; }

    [Required]
    public string QuestionText { get; set; } = default!;

    public string? ImageUrl { get; set; }
    public string? AudioUrl { get; set; }
    public string? RequiredWords { get; set; }
    public decimal? Points { get; set; }
}

public class QuestionDto
{
    public Guid QuestionId { get; set; }
    public Guid TestId { get; set; }
    public Section Section { get; set; }
    public QuestionType QuestionType { get; set; }
    public int QuestionNumber { get; set; }
    public string QuestionText { get; set; } = default!;
    public string? ImageUrl { get; set; }
    public string? AudioUrl { get; set; }
    public string? RequiredWords { get; set; }
    public decimal? Points { get; set; }
    public IReadOnlyList<QuestionOptionDto> Options { get; set; } = Array.Empty<QuestionOptionDto>();
}

public class CreateQuestionOptionRequest
{
    //[Required, MaxLength(1)]
    [Required]
    public char OptionLabel { get; set; }

    [Required]
    public string OptionText { get; set; } = default!;

    public bool IsCorrect { get; set; }
}

public class UpdateQuestionOptionRequest
{
    //[Required, MaxLength(1)]
    [Required]
    public char OptionLabel { get; set; }

    [Required]
    public string OptionText { get; set; } = default!;

    public bool IsCorrect { get; set; }
}

public class QuestionOptionDto
{
    public Guid QuestionOptionId { get; set; }
    public Guid QuestionId { get; set; }
    public char OptionLabel { get; set; }
    public string OptionText { get; set; } = default!;

    /// <summary>
    /// CHỈ trả về true/false thật (đây là Answer Key) khi caller là ADMIN/RATER.
    /// Với CANDIDATE trong lúc đang làm bài, service phải trả false cho mọi option
    /// (ẩn đáp án) — enforce ở tầng Controller/Service khi module Exam Room được xây (Part 4),
    /// KHÔNG phải trách nhiệm của DTO này.
    /// </summary>
    public bool IsCorrect { get; set; }
}
