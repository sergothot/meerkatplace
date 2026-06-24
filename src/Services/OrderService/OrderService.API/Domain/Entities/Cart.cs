using Common.Shared.Domain.Enums;

namespace OrderService.API.Domain.Entities;

public class Cart
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid BuyerId { get; set; }
    public List<CartItem> Items { get; set; } = new();

    public void AddItem(Guid productId, int quantity, decimal unitPrice, Currency currency)
    {
        if (quantity <= 0)
        {
            throw new InvalidOperationException("Quantity must be greater than zero.");
        }

        if (unitPrice <= 0)
        {
            throw new InvalidOperationException("Unit price must be greater than zero.");
        }

        Items.Add(new CartItem
        {
            ProductId = productId,
            UnitPrice = unitPrice,
            Currency = currency
        });

        Items.Last().SetQuantity(quantity);
    }

    public bool TryUpdateItemQuantity(Guid itemId, int quantity)
    {
        if (quantity <= 0)
        {
            throw new InvalidOperationException("Quantity must be greater than zero.");
        }

        var item = Items.FirstOrDefault(i => i.Id == itemId);
        if (item is null)
        {
            return false;
        }

        item.SetQuantity(quantity);
        return true;
    }

    public bool TryRemoveItem(Guid itemId)
    {
        var item = Items.FirstOrDefault(i => i.Id == itemId);
        if (item is null)
        {
            return false;
        }

        Items.Remove(item);
        return true;
    }

    public bool IsEmpty() => Items.Count == 0;

    public Currency? TryGetSingleCurrency()
    {
        var currencies = Items.Select(i => i.Currency).Distinct().ToArray();
        return currencies.Length == 1 ? currencies[0] : null;
    }

    public decimal CalculateTotal() => Items.Sum(i => i.Quantity * i.UnitPrice);

    public void ClearItems() => Items.Clear();
}

