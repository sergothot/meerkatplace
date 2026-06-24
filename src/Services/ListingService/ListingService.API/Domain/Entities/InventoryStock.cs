namespace ListingService.API.Domain.Entities;

public class InventoryStock
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public int Reserved { get; set; }

    public bool TryReserve(int amount)
    {
        if (amount <= 0)
        {
            throw new InvalidOperationException("Reserve amount must be greater than zero.");
        }

        if (Quantity - Reserved < amount)
        {
            return false;
        }

        Reserved += amount;
        return true;
    }

    public void Release(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        Reserved = Math.Max(0, Reserved - amount);
    }
}
