using OrderService.API.Domain.Entities;

namespace OrderService.API.Application.Abstractions;

public interface ICartRepository
{
    Task<Cart?> GetByBuyerIdAsync(Guid buyerId, CancellationToken cancellationToken = default);

    Task<Cart?> GetByBuyerIdWithItemsAsync(Guid buyerId, CancellationToken cancellationToken = default);

    Task AddAsync(Cart cart, CancellationToken cancellationToken = default);

    Task AddItemAsync(CartItem item, CancellationToken cancellationToken = default);

    void Remove(Cart cart);

    void RemoveItem(CartItem item);

    void RemoveItems(IEnumerable<CartItem> items);
}
