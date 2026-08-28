using System.ComponentModel.DataAnnotations;
using Toeic.Application.Common.Dtos;
using Toeic.Domain.Enums;

namespace Toeic.Application.ExamManagement.Dtos;

public class CreateExamRequest
{
    [Required, MaxLength(50)]
    public string ExamCode { get; set; } = default!;

    [Required, MaxLength(200)]
    public string ExamName { get; set; } = default!;

    public string? Description { get; set; }

    [Required]
    public DateOnly ExamDate { get; set; }

    [Required]
    public DateTime StartTime { get; set; }

    [Required]
    public DateTime EndTime { get; set; }
}

public class UpdateExamRequest
{
    [Required, MaxLength(200)]
    public string ExamName { get; set; } = default!;

    public string? Description { get; set; }

    [Required]
    public DateOnly ExamDate { get; set; }

    [Required]
    public DateTime StartTime { get; set; }

    [Required]
    public DateTime EndTime { get; set; }
}

/// <summary>Chuyển trạng thái Exam theo state machine: DRAFT -> SCHEDULED -> ONGOING -> COMPLETED, hoặc -> CANCELLED từ bất kỳ trạng thái nào chưa COMPLETED.</summary>
public class ChangeExamStatusRequest
{
    [Required]
    public ExamStatus Status { get; set; }
}

public class AssignTestToExamRequest
{
    [Required]
    public Guid TestId { get; set; }
}

public class ExamDto
{
    public Guid ExamId { get; set; }
    public string ExamCode { get; set; } = default!;
    public string ExamName { get; set; } = default!;
    public string? Description { get; set; }
    public DateOnly ExamDate { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public ExamStatus Status { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public IReadOnlyList<ExamTestSummaryDto> AssignedTests { get; set; } = Array.Empty<ExamTestSummaryDto>();
}

public class ExamListItemDto
{
    public Guid ExamId { get; set; }
    public string ExamCode { get; set; } = default!;
    public string ExamName { get; set; } = default!;
    public DateOnly ExamDate { get; set; }
    public ExamStatus Status { get; set; }
    public int AssignedTestCount { get; set; }
}

public class ExamTestSummaryDto
{
    public Guid TestId { get; set; }
    public string TestCode { get; set; } = default!;
    public string TestName { get; set; } = default!;
    public bool IsActive { get; set; }
}

public class ExamListQuery : PageQuery
{
    public ExamStatus? Status { get; set; }
    public string? Search { get; set; }
}
