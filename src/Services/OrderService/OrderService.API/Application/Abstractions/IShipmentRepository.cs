using OrderService.API.Domain.Entities;

namespace OrderService.API.Application.Abstractions;

public interface IShipmentRepository
{
    Task AddAsync(Shipment shipment, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Shipment>> ListByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
}
