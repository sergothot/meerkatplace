using Common.Shared.Domain.Enums;
using ListingService.API.Domain.Enums;

namespace ListingService.API.Domain.Entities;

public class Product
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid SellerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public Currency Currency { get;  set; }
    public DeliveryType DeliveryType { get;  set; }
    public ProductStatus Status { get;  set; }

    public void Create()
    {
        Status = ProductStatus.Draft;
    }
    public void Publish()
    {
        Status = ProductStatus.Active;
    }

    public void Deactivate()
    {
        Status = ProductStatus.Inactive;
    }

    public void Archive()
    {
        Status = ProductStatus.Archived;
    }
}
