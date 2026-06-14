using Common.Shared.Domain.Enums;
using OrderService.API.Domain.Enums;

namespace OrderService.API.Domain.Entities;

public class Order
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid BuyerId { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Draft;
    public decimal TotalAmount { get; set; }
    public Currency Currency { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public List<OrderItem> Items { get; set; } = new();
}


