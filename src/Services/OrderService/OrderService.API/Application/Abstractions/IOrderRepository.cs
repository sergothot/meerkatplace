using OrderService.API.Domain.Entities;

namespace OrderService.API.Application.Abstractions;

public interface IOrderRepository
{
    Task AddAsync(Order order, CancellationToken cancellationToken = default);

    Task<Order?> GetByIdAsync(
        Guid orderId,
        bool includeItems = false,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Order>> ListByBuyerAsync(Guid buyerId, CancellationToken cancellationToken = default);

    Task<Order?> GetByIdForBuyerAsync(
        Guid orderId,
        Guid buyerId,
        bool includeItems = false,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsForBuyerAsync(Guid orderId, Guid buyerId, CancellationToken cancellationToken = default);

    void Update(Order order);
}
