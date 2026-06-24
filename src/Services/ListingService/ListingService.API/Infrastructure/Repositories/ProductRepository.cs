using Common.Shared.Application.Interfaces;
using Common.Shared.Domain.Enums;
using ListingService.API.Application.Abstractions;
using ListingService.API.Application.Catalog;
using ListingService.API.Domain.Entities;
using ListingService.API.Domain.Enums;
using ListingService.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ListingService.API.Infrastructure.Repositories;

public sealed class ProductRepository(ListingDbContext db) : IProductRepository
{
    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await db.Products
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> SearchAsync(ProductFilter filter, CancellationToken cancellationToken = default)
    {
        var query = db.Products.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Query))
        {
            query = query.Where(p =>
                p.Name.Contains(filter.Query) ||
                p.Description.Contains(filter.Query));
        }

        if (filter.MinPrice.HasValue)
        {
            query = query.Where(p => p.Price >= filter.MinPrice.Value);
        }

        if (filter.MaxPrice.HasValue)
        {
            query = query.Where(p => p.Price <= filter.MaxPrice.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Currency) && TryParseCurrency(filter.Currency, out var currency))
        {
            query = query.Where(p => p.Currency == currency);
        }

        if (!string.IsNullOrWhiteSpace(filter.DeliveryType) && TryParseDeliveryType(filter.DeliveryType, out var deliveryType))
        {
            query = query.Where(p => p.DeliveryType == deliveryType);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Product entity, CancellationToken cancellationToken = default)
    {
        await db.Products.AddAsync(entity, cancellationToken);
    }

    public void Update(Product entity)
    {
        db.Products.Update(entity);
    }

    public void Remove(Product entity)
    {
        db.Products.Remove(entity);
    }

    private static bool TryParseCurrency(string value, out Currency currency)
    {
        return Enum.TryParse(value, true, out currency);
    }

    private static bool TryParseDeliveryType(string value, out DeliveryType deliveryType)
    {
        return Enum.TryParse(value, true, out deliveryType);
    }
}
