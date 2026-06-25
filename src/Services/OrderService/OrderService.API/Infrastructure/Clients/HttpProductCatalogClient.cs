using System.Text.Json;
using OrderService.API.Application.Abstractions;

namespace OrderService.API.Infrastructure.Clients;

public sealed class HttpProductCatalogClient(HttpClient httpClient) : IProductCatalogClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<ProductPricingDto?> GetProductPricingAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync($"/products/{productId}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var payload = await JsonSerializer.DeserializeAsync<ProductDetailsResponse>(stream, JsonOptions, cancellationToken);
        if (payload is null)
        {
            return null;
        }

        return new ProductPricingDto(payload.Price, payload.Currency);
    }

    private sealed record ProductDetailsResponse(Guid Id, decimal Price, string Currency);
}