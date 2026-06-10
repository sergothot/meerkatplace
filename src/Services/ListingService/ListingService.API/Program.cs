using ListingService.API.Application.Catalog;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
var app = builder.Build();
var products = new List<ProductDto>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/", () => "Hello World!");
app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "listing-service" }));
app.MapGet("/products", (
    string? q,
    decimal? minPrice,
    decimal? maxPrice,
    string? currency,
    string? deliveryType) =>
{
    var filter = new ProductFilter(q, minPrice, maxPrice, currency, deliveryType);
    var errors = CatalogRequestValidator.ValidateFilter(filter);
    if (errors.Count > 0)
    {
        return Results.ValidationProblem(errors);
    }

    IEnumerable<ProductDto> query = products;

    if (!string.IsNullOrWhiteSpace(filter.Query))
    {
        query = query.Where(p =>
            p.Name.Contains(filter.Query, StringComparison.OrdinalIgnoreCase) ||
            p.Description.Contains(filter.Query, StringComparison.OrdinalIgnoreCase));
    }

    if (filter.MinPrice.HasValue)
    {
        query = query.Where(p => p.Price >= filter.MinPrice.Value);
    }

    if (filter.MaxPrice.HasValue)
    {
        query = query.Where(p => p.Price <= filter.MaxPrice.Value);
    }

    if (!string.IsNullOrWhiteSpace(filter.Currency))
    {
        query = query.Where(p =>
            string.Equals(p.Currency, filter.Currency, StringComparison.OrdinalIgnoreCase));
    }

    if (!string.IsNullOrWhiteSpace(filter.DeliveryType))
    {
        query = query.Where(p =>
            string.Equals(p.DeliveryType, filter.DeliveryType, StringComparison.OrdinalIgnoreCase));
    }

    return Results.Ok(query.ToList());
});
app.MapGet("/products/{id:guid}", (Guid id) =>
{
    var product = products.FirstOrDefault(p => p.Id == id);
    return product is null ? Results.NotFound() : Results.Ok(product);
});

app.Run();
