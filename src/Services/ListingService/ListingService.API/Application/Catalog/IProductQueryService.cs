namespace ListingService.API.Application.Catalog;

public interface IProductQueryService
{
    Task<IResult> GetProductsAsync(ProductFilter filter);

    Task<IResult> GetProductByIdAsync(Guid id);
}
