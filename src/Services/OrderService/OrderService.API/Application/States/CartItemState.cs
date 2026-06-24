namespace OrderService.API.Application.States;

public sealed class CartItemState
{
    public Guid ItemId { get; init; } = Guid.NewGuid();
    public Guid ProductId { get; init; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; init; }
    public string Currency { get; init; } = "RUB";
}