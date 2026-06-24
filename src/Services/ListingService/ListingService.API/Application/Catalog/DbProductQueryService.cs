using ListingService.API.Application.Abstractions;

namespace ListingService.API.Application.Catalog;

public sealed class DbProductQueryService(IProductRepository products) : IProductQueryService
{
    public async Task<IResult> GetProductsAsync(ProductFilter filter)
    {
        var errors = CatalogRequestValidator.ValidateFilter(filter);
        if (errors.Count > 0)
            return Results.ValidationProblem(errors);

        var matchedProducts = await products.SearchAsync(filter);

        var response = matchedProducts
            .Select(p => new ProductDto(
                p.Id,
                p.Name,
                p.Description,
                p.Price,
                p.Currency.ToString(),
                p.DeliveryType.ToString()))
            .ToList();

        return Results.Ok(response);
    }

    public async Task<IResult> GetProductByIdAsync(Guid id)
    {
        var product = await products.GetByIdAsync(id);
        return product is null
            ? Results.NotFound()
            : Results.Ok(new ProductDto(
                product.Id,
                product.Name,
                product.Description,
                product.Price,
                product.Currency.ToString(),
                product.DeliveryType.ToString()));
    }
}
