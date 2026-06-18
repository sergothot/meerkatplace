namespace OrderService.API.Application.States;

public sealed class CartState
{
    public Guid CartId { get; init; } = Guid.NewGuid();
    public Guid BuyerId { get; init; }
    public List<CartItemState> Items { get; } = new();
}