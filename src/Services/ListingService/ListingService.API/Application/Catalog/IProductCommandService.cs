namespace ListingService.API.Application.Catalog;

public interface IProductCommandService
{
    Task<IResult> CreateProductAsync(CreateProductRequest request);

    Task<IResult> UpdateProductAsync(Guid productId, UpdateProductRequest request);

    Task<IResult> UpdateStockAsync(Guid productId, UpdateStockRequest request);

    Task<IResult> RemoveProductAsync(Guid productId);
}
