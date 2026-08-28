using System.ComponentModel.DataAnnotations;
using Toeic.Application.Common.Dtos;
using Toeic.Domain.Enums;

namespace Toeic.Application.ExamManagement.Dtos;

public class CreateTestRequest
{
    [Required, MaxLength(50)]
    public string TestCode { get; set; } = default!;

    [Required, MaxLength(200)]
    public string TestName { get; set; } = default!;

    [Required, MaxLength(20)]
    public string Version { get; set; } = default!;

    public string? Description { get; set; }

    [Range(1, int.MaxValue)]
    public int ListeningDurationSeconds { get; set; }

    [Range(1, int.MaxValue)]
    public int ReadingDurationSeconds { get; set; }

    [Range(1, int.MaxValue)]
    public int WritingDurationSeconds { get; set; }
}

public class UpdateTestRequest
{
    [Required, MaxLength(200)]
    public string TestName { get; set; } = default!;

    [Required, MaxLength(20)]
    public string Version { get; set; } = default!;

    public string? Description { get; set; }

    [Range(1, int.MaxValue)]
    public int ListeningDurationSeconds { get; set; }

    [Range(1, int.MaxValue)]
    public int ReadingDurationSeconds { get; set; }

    [Range(1, int.MaxValue)]
    public int WritingDurationSeconds { get; set; }
}

public class TestDto
{
    public Guid TestId { get; set; }
    public string TestCode { get; set; } = default!;
    public string TestName { get; set; } = default!;
    public string Version { get; set; } = default!;
    public string? Description { get; set; }
    public int ListeningDurationSeconds { get; set; }
    public int ReadingDurationSeconds { get; set; }
    public int WritingDurationSeconds { get; set; }
    public TestStatus Status { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int QuestionCount { get; set; }
    public int ListeningQuestionCount { get; set; }
    public int ReadingQuestionCount { get; set; }
    public int WritingQuestionCount { get; set; }
}

public class TestListItemDto
{
    public Guid TestId { get; set; }
    public string TestCode { get; set; } = default!;
    public string TestName { get; set; } = default!;
    public string Version { get; set; } = default!;
    public TestStatus Status { get; set; }
    public int QuestionCount { get; set; }
}

public class TestListQuery : PageQuery
{
    public TestStatus? Status { get; set; }
    public string? Search { get; set; }
}
