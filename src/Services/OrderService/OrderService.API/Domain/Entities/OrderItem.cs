using Common.Shared.Domain.Enums;
using OrderService.API.Domain.Enums;

namespace OrderService.API.Domain.Entities;

public class OrderItem
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public Guid ProductId { get; private set; }
    public Guid SellerId { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public Currency Currency { get; private set; }
    public FulfillmentStatus FulfillmentStatus { get; private set; } = FulfillmentStatus.Pending;

    public static OrderItem Create(Guid productId, Guid sellerId, int quantity, decimal unitPrice, Currency currency)
    {
        if (quantity <= 0)
        {
            throw new InvalidOperationException("Order item quantity must be greater than zero.");
        }

        if (unitPrice <= 0)
        {
            throw new InvalidOperationException("Order item unit price must be greater than zero.");
        }

        return new OrderItem
        {
            ProductId = productId,
            SellerId = sellerId,
            Quantity = quantity,
            UnitPrice = unitPrice,
            Currency = currency,
            FulfillmentStatus = FulfillmentStatus.Pending
        };
    }

    public void MarkDelivered()
    {
        if (FulfillmentStatus != FulfillmentStatus.Pending)
        {
            throw new InvalidOperationException("Only pending order items can be marked delivered.");
        }

        FulfillmentStatus = FulfillmentStatus.Delivered;
    }
}