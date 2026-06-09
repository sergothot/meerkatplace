namespace ListingService.API.Domain.Entities;

public class InventoryStock
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public int Reserved { get; set; }
}
