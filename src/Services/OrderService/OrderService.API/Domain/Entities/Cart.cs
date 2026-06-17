using Common.Shared.Domain.Enums;

namespace OrderService.API.Domain.Entities;


public class Cart
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid BuyerId { get; set; }
    public List<CartItem> Items { get; set; } = new();
    public decimal TotalAmount { get; set; }
    public Currency Currency { get; set; }
}

