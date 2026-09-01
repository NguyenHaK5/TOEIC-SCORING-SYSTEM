using Toeic.Domain.Enums; // ASSUMPTION: QuestionType and RubricSetStatus live here (same enum QuestionType
                           // already used by Questions per Phase 0 schema E.2.6 / E.2.10). Adjust the
                           // `using` if your actual enums live in a different namespace/file.

namespace Toeic.Application.RubricManagement.Dtos;

public class RubricDto
{
    public Guid RubricId { get; set; }
    public Guid RubricSetId { get; set; }
    public string Criterion { get; set; } = default!;
    public int Level { get; set; }
    public decimal MinScore { get; set; }
    public decimal MaxScore { get; set; }
    public string? Description { get; set; }
    public string? ScoringRule { get; set; }
    public decimal Weight { get; set; }
}

public class RubricSetDto
{
    public Guid RubricSetId { get; set; }
    public string Name { get; set; } = default!;
    public QuestionType QuestionType { get; set; }
    public string? Description { get; set; }
    public string Version { get; set; } = default!;
    public RubricSetStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<RubricDto> Rubrics { get; set; } = new();
}

public class CreateRubricSetRequest
{
    public string Name { get; set; } = default!;
    public QuestionType QuestionType { get; set; }
    public string? Description { get; set; }
    public string Version { get; set; } = default!;
}

public class UpdateRubricSetRequest
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
}

public class CreateRubricRequest
{
    public string Criterion { get; set; } = default!;
    public int Level { get; set; }
    public decimal MinScore { get; set; }
    public decimal MaxScore { get; set; }
    public string? Description { get; set; }
    public string? ScoringRule { get; set; }
    public decimal Weight { get; set; }
}

public class UpdateRubricRequest
{
    public string Criterion { get; set; } = default!;
    public int Level { get; set; }
    public decimal MinScore { get; set; }
    public decimal MaxScore { get; set; }
    public string? Description { get; set; }
    public string? ScoringRule { get; set; }
    public decimal Weight { get; set; }
}
