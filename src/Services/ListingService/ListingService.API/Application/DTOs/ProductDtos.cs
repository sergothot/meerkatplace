namespace ListingService.API.Application.DTOs;

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

public record CreateProductRequest(
    Guid SellerId,
    string Name,
    string Description,
    decimal Price,
    string Currency,
    string DeliveryType,
    int? StockQuantity);

public record UpdateProductRequest(
    string? Name,
    string? Description,
    decimal? Price,
    string? Currency,
    string? DeliveryType,
    string? Status);

public record UpdateStockRequest(int Quantity, int Reserved);
