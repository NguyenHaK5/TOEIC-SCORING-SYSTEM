// File: backend/src/Toeic.Application/RubricManagement/Dtos/BulkRubricDtos.cs — MỚI
using Toeic.Application.Common.Dtos;

namespace Toeic.Application.RubricManagement.Dtos;

public record BulkRubricRowDto(
    int RowNumber,
    string Criterion,
    int Level,
    decimal MinScore,
    decimal MaxScore,
    string? Description,
    string? ScoringRule,
    decimal Weight
);

public record BulkCreateRubricsRequestDto(List<BulkRubricRowDto> Rows);

public record BulkCreateRubricsResultDto(
    bool Success,
    int TotalRows,
    int CreatedCount,
    List<BulkRowErrorDto> Errors
);

public record BulkUpdateRubricRowDto(
    int RowNumber,
    Guid RubricId,
    string Criterion,
    int Level,
    decimal MinScore,
    decimal MaxScore,
    string? Description,
    string? ScoringRule,
    decimal Weight
);

public record BulkUpdateRubricsRequestDto(List<BulkUpdateRubricRowDto> Rows);

public record BulkUpdateRubricsResultDto(
    bool Success,
    int TotalRows,
    int UpdatedCount,
    List<BulkRowErrorDto> Errors
);

public record RubricExportRowDto(
    Guid RubricId,
    string Criterion,
    int Level,
    decimal MinScore,
    decimal MaxScore,
    string? Description,
    string? ScoringRule,
    decimal Weight
);