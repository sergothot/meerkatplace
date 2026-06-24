using Microsoft.EntityFrameworkCore;
using OrderService.API.Application.Abstractions;
using OrderService.API.Domain.Entities;
using OrderService.API.Infrastructure.Persistence;

namespace OrderService.API.Infrastructure.Repositories;

public sealed class ShipmentRepository(OrderDbContext db) : IShipmentRepository
{
    public async Task AddAsync(Shipment shipment, CancellationToken cancellationToken = default)
    {
        await db.Shipments.AddAsync(shipment, cancellationToken);
    }

    public async Task<IReadOnlyList<Shipment>> ListByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return await db.Shipments
            .Where(s => s.OrderId == orderId)
            .ToListAsync(cancellationToken);
    }
}
