using Common.Shared.Application.Interfaces;
using ListingService.API.Application.Catalog;
using ListingService.API.Domain.Entities;

namespace ListingService.API.Application.Abstractions;

public interface IProductRepository : IRepository<Product>
{
    Task<IReadOnlyList<Product>> SearchAsync(ProductFilter filter, CancellationToken cancellationToken = default);
}
