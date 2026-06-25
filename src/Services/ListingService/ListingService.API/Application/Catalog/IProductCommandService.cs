namespace ListingService.API.Application.Catalog;

public interface IProductCommandService
{
    Task<IResult> CreateProductAsync(HttpContext httpContext, CreateProductRequest request);

    Task<IResult> UpdateProductAsync(HttpContext httpContext, Guid productId, UpdateProductRequest request);

    Task<IResult> UpdateStockAsync(HttpContext httpContext, Guid productId, UpdateStockRequest request);

    Task<IResult> RemoveProductAsync(HttpContext httpContext, Guid productId);
}
