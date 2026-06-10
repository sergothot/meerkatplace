namespace OrderService.API.Domain.Entities;

public class Cart
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid BuyerId { get; set; }
    public List<CartItem> Items { get; set; } = new();
}

public class CartItem
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string Currency { get; set; } = "RUB";
}
