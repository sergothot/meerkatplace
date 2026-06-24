using Microsoft.EntityFrameworkCore;
using OrderService.API.Application.Abstractions;
using OrderService.API.Domain.Entities;
using OrderService.API.Infrastructure.Persistence;

namespace OrderService.API.Infrastructure.Repositories;

public sealed class OrderRepository(OrderDbContext db) : IOrderRepository
{
    public async Task AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        await db.Orders.AddAsync(order, cancellationToken);
    }

    public async Task<Order?> GetByIdAsync(
        Guid orderId,
        bool includeItems = false,
        CancellationToken cancellationToken = default)
    {
        var query = db.Orders.AsQueryable();
        if (includeItems)
        {
            query = query.Include(o => o.Items);
        }

        return await query.FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
    }

    public async Task<IReadOnlyList<Order>> ListByBuyerAsync(Guid buyerId, CancellationToken cancellationToken = default)
    {
        return await db.Orders
            .Where(o => o.BuyerId == buyerId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Order?> GetByIdForBuyerAsync(
        Guid orderId,
        Guid buyerId,
        bool includeItems = false,
        CancellationToken cancellationToken = default)
    {
        var query = db.Orders.AsQueryable();
        if (includeItems)
        {
            query = query.Include(o => o.Items);
        }

        return await query.FirstOrDefaultAsync(
            o => o.Id == orderId && o.BuyerId == buyerId,
            cancellationToken);
    }

    public async Task<bool> ExistsForBuyerAsync(Guid orderId, Guid buyerId, CancellationToken cancellationToken = default)
    {
        return await db.Orders.AnyAsync(o => o.Id == orderId && o.BuyerId == buyerId, cancellationToken);
    }

    public void Update(Order order)
    {
        db.Orders.Update(order);
    }
}
