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

        productsGroup.MapPost("", (HttpContext httpContext, CreateProductRequest request, IProductCommandService commands) =>
            commands.CreateProductAsync(httpContext, request))
            .RequireAuthorization()
            .WithSummary("Create product")
            .WithDescription("Creates a product and optional initial stock quantity.");

        productsGroup.MapPatch("/{id:guid}", (HttpContext httpContext, Guid id, UpdateProductRequest request, IProductCommandService commands) =>
            commands.UpdateProductAsync(httpContext, id, request))
            .RequireAuthorization()
            .WithSummary("Update product")
            .WithDescription("Updates mutable product fields such as name, price, and status.");

        productsGroup.MapPatch("/{id:guid}/stock", (HttpContext httpContext, Guid id, UpdateStockRequest request, IProductCommandService commands) =>
            commands.UpdateStockAsync(httpContext, id, request))
            .RequireAuthorization();

        productsGroup.MapDelete("/{id:guid}", (HttpContext httpContext, Guid id, IProductCommandService commands) =>
            commands.RemoveProductAsync(httpContext, id))
            .RequireAuthorization();
    }
}
