using ListingService.API.Application.Abstractions;
using ListingService.API.Domain.Entities;
using ListingService.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ListingService.API.Infrastructure.Repositories;

public sealed class InventoryStockRepository(ListingDbContext db) : IInventoryStockRepository
{
    public async Task<InventoryStock?> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return await db.InventoryStocks
            .FirstOrDefaultAsync(s => s.ProductId == productId, cancellationToken);
    }

    public async Task AddAsync(InventoryStock stock, CancellationToken cancellationToken = default)
    {
        await db.InventoryStocks.AddAsync(stock, cancellationToken);
    }

    public void Update(InventoryStock stock)
    {
        db.InventoryStocks.Update(stock);
    }
}
