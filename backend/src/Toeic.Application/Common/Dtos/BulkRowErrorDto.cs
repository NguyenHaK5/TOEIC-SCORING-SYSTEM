namespace Toeic.Application.Common.Dtos;

public record BulkRowErrorDto(int RowNumber, List<string> Errors);