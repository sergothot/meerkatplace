namespace OrderService.API.Application.Abstractions;

public interface IProductCatalogClient
{
    Task<ProductPricingDto?> GetProductPricingAsync(Guid productId, CancellationToken cancellationToken = default);
}

public sealed record ProductPricingDto(decimal Price, string Currency);