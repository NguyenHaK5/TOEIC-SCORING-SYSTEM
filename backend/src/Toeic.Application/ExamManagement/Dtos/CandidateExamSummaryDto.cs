// Toeic.Application/ExamManagement/Dtos/CandidateExamSummaryDto.cs
namespace Toeic.Application.ExamManagement.Dtos;

public record CandidateExamSummaryDto(
    Guid ExamId,
    string ExamCode,
    string ExamName,
    string? Description,
    DateOnly ExamDate,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    string Status // "SCHEDULED" | "ONGOING"
);