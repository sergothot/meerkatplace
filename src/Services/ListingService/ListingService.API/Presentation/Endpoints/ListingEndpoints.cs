using ListingService.API.Application.Catalog;

namespace ListingService.API.Presentation.Endpoints;

public static class ListingEndpoints
{
    public static void MapListingEndpoints(this WebApplication app)
    {
        app.MapGet("/", () => "Hello World!");
        app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "listing-service" }));

        var productsGroup = app.MapGroup("/products");
        productsGroup.MapGet("", (
            string? q,
            decimal? minPrice,
            decimal? maxPrice,
            string? currency,
            string? deliveryType,
            IProductQueryService products) =>
        {
            var filter = new ProductFilter(q, minPrice, maxPrice, currency, deliveryType);
            return products.GetProductsAsync(filter);
        })
        .WithSummary("List products")
        .WithDescription("Returns product catalog with optional text/price/currency/delivery filters.");

        productsGroup.MapGet("/{id:guid}", (Guid id, IProductQueryService products) =>
            products.GetProductByIdAsync(id))
            .WithSummary("Get product")
            .WithDescription("Returns details for one product by id.");

        productsGroup.MapPost("", (CreateProductRequest request, IProductCommandService commands) =>
            commands.CreateProductAsync(request))
            .WithSummary("Create product")
            .WithDescription("Creates a product and optional initial stock quantity.");

        productsGroup.MapPatch("/{id:guid}", (Guid id, UpdateProductRequest request, IProductCommandService commands) =>
            commands.UpdateProductAsync(id, request))
            .WithSummary("Update product")
            .WithDescription("Updates mutable product fields such as name, price, and status.");

        productsGroup.MapPatch("/{id:guid}/stock", (Guid id, UpdateStockRequest request, IProductCommandService commands) =>
            commands.UpdateStockAsync(id, request));

        productsGroup.MapDelete("/{id:guid}", (Guid id, IProductCommandService commands) =>
            commands.RemoveProductAsync(id));
    }
}
