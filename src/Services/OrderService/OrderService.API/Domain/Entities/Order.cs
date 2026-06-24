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

    public static Order CreatePlaced(Guid buyerId, Currency currency, IReadOnlyList<OrderItem> items)
    {
        if (items.Count == 0)
        {
            throw new InvalidOperationException("Order must contain at least one item.");
        }

        return new Order
        {
            BuyerId = buyerId,
            Status = OrderStatus.Placed,
            Currency = currency,
            TotalAmount = items.Sum(i => i.Quantity * i.UnitPrice),
            Items = items.ToList()
        };
    }

    public void MarkPaid()
    {
        if (Status != OrderStatus.Placed)
        {
            throw new InvalidOperationException("Only placed orders can transition to paid.");
        }

        Status = OrderStatus.Paid;
    }

    public void Cancel()
    {
        if (Status is OrderStatus.Paid or OrderStatus.Fulfilled or OrderStatus.Completed)
        {
            throw new InvalidOperationException("Paid, fulfilled, or completed orders cannot be cancelled.");
        }

        Status = OrderStatus.Cancelled;
    }

    public void MarkPaymentFailed()
    {
        if (Status != OrderStatus.Placed)
        {
            throw new InvalidOperationException("Only placed orders can be failed by payment.");
        }

        Status = OrderStatus.Cancelled;
    }
}


