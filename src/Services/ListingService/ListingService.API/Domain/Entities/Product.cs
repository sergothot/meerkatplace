namespace ListingService.API.Domain.Entities;
using Common.Shared.Domain.Enums;
using ListingService.API.Domain.Enums;

public class Product
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid SellerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public Currency Currency { get; private set; }
    public DeliveryType DeliveryType { get; private set; }
    public ProductStatus Status { get; private set; }
}
