using ListingService.API.Domain.Entities;

namespace ListingService.API.Application.Abstractions.Repositories;

public interface IInventoryRepository
{
    Task<InventoryStock?> GetByProductIdAsync(
        Guid productId,
        CancellationToken cancellationToken = default);

    Task<int> GetAvailableStockAsync(
        Guid productId,
        CancellationToken cancellationToken = default);

    Task<bool> ReserveStockAsync(
        Guid productId,
        int quantity,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        InventoryStock inventory,
        CancellationToken cancellationToken = default);
}