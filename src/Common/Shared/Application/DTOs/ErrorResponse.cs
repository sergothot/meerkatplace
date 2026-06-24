namespace Common.Shared.Application.DTOs;

public sealed record ValidationErrorDetail(string Field, string Message);

public sealed record ErrorBody(
    string Code,
    string Message,
    IReadOnlyList<ValidationErrorDetail>? Details = null);

public sealed record ErrorResponse(ErrorBody Error);
