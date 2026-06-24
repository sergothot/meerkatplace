using ListingService.API.Domain.Entities;

namespace ListingService.API.Application.Abstractions;

public interface IInventoryStockRepository
{
    Task<InventoryStock?> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);

    Task AddAsync(InventoryStock stock, CancellationToken cancellationToken = default);

    void Update(InventoryStock stock);
}
