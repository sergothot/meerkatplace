namespace Common.Shared.Application.DTOs;

public sealed record PaginationMeta(
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record PaginatedResponse<T>(
    IReadOnlyList<T> Items,
    PaginationMeta Pagination);
