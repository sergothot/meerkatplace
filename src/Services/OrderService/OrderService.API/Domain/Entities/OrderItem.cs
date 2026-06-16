using Common.Shared.Domain.Enums;
using OrderService.API.Domain.Enums;

namespace OrderService.API.Domain.Entities;

public class OrderItem
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }
    public Guid SellerId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public Currency Currency { get; set; }
    public FulfillmentStatus FulfillmentStatus { get; set; } = FulfillmentStatus.Pending;
}