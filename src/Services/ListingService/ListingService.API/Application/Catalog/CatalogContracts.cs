namespace ListingService.API.Application.Catalog;

public record ProductDto(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    string Currency,
    string DeliveryType);

public record ProductFilter(
    string? Query,
    decimal? MinPrice,
    decimal? MaxPrice,
    string? Currency,
    string? DeliveryType);
