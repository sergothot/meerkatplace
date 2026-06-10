namespace ListingService.API.Domain.Entities;

public class Product
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid SellerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "RUB";
    public string DeliveryType { get; set; } = "Physical";
    public string Status { get; set; } = "Active";
}
