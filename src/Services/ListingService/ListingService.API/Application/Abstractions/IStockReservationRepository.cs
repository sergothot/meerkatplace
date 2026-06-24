using ListingService.API.Domain.Entities;

namespace ListingService.API.Application.Abstractions;

public interface IStockReservationRepository
{
    Task<StockReservation?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);

    Task AddAsync(StockReservation reservation, CancellationToken cancellationToken = default);

    void Update(StockReservation reservation);
}