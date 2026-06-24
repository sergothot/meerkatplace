using Microsoft.EntityFrameworkCore;
using OrderService.API.Application.Abstractions;
using OrderService.API.Domain.Entities;
using OrderService.API.Infrastructure.Persistence;

namespace OrderService.API.Infrastructure.Repositories;

public sealed class CartRepository(OrderDbContext db) : ICartRepository
{
    public async Task<Cart?> GetByBuyerIdAsync(Guid buyerId, CancellationToken cancellationToken = default)
    {
        return await db.Carts
            .FirstOrDefaultAsync(c => c.BuyerId == buyerId, cancellationToken);
    }

    public async Task<Cart?> GetByBuyerIdWithItemsAsync(Guid buyerId, CancellationToken cancellationToken = default)
    {
        return await db.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.BuyerId == buyerId, cancellationToken);
    }

    public async Task AddAsync(Cart cart, CancellationToken cancellationToken = default)
    {
        await db.Carts.AddAsync(cart, cancellationToken);
    }

    public async Task AddItemAsync(CartItem item, CancellationToken cancellationToken = default)
    {
        await db.CartItems.AddAsync(item, cancellationToken);
    }

    public void Remove(Cart cart)
    {
        db.Carts.Remove(cart);
    }

    public void RemoveItem(CartItem item)
    {
        db.CartItems.Remove(item);
    }

    public void RemoveItems(IEnumerable<CartItem> items)
    {
        db.CartItems.RemoveRange(items);
    }
}
