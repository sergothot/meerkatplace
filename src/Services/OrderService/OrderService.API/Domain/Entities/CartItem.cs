using Common.Shared.Domain.Enums;

namespace OrderService.API.Domain.Entities;

public class CartItem
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid CartId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; set; }
    public Currency Currency { get; set; }

    public void SetQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new InvalidOperationException("Quantity must be greater than zero.");
        }

        Quantity = quantity;
    }
}