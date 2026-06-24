using ListingService.API.Application.Abstractions;
using ListingService.API.Domain.Entities;
using ListingService.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ListingService.API.Infrastructure.Repositories;

public sealed class StockReservationRepository(ListingDbContext db) : IStockReservationRepository
{
    public async Task<StockReservation?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return await db.StockReservations
            .FirstOrDefaultAsync(r => r.OrderId == orderId, cancellationToken);
    }

    public async Task AddAsync(StockReservation reservation, CancellationToken cancellationToken = default)
    {
        await db.StockReservations.AddAsync(reservation, cancellationToken);
    }

    public void Update(StockReservation reservation)
    {
        db.StockReservations.Update(reservation);
    }
}