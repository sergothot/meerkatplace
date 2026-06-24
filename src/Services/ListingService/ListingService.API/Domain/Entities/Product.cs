using Common.Shared.Domain.Enums;
using ListingService.API.Domain.Enums;

namespace ListingService.API.Domain.Entities;

public class Product
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid SellerId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public Currency Currency { get; private set; }
    public DeliveryType DeliveryType { get; private set; }
    public ProductStatus Status { get; private set; } = ProductStatus.Draft;

    public static Product Create(Guid sellerId, string name, string description, decimal price, Currency currency, DeliveryType deliveryType)
    {
        ValidateName(name);
        ValidatePrice(price);

        return new Product
        {
            SellerId = sellerId,
            Name = name.Trim(),
            Description = description.Trim(),
            Price = price,
            Currency = currency,
            DeliveryType = deliveryType,
            Status = ProductStatus.Draft
        };
    }

    public void UpdateDetails(string name, string description, decimal price)
    {
        ValidateName(name);
        ValidatePrice(price);

        Name = name.Trim();
        Description = description.Trim();
        Price = price;
    }

    public void Activate()
    {
        if (Status == ProductStatus.Archived)
        {
            throw new InvalidOperationException("Archived products cannot be activated.");
        }

        Status = ProductStatus.Active;
    }

    public void Deactivate()
    {
        if (Status == ProductStatus.Archived)
        {
            throw new InvalidOperationException("Archived products cannot be deactivated.");
        }

        Status = ProductStatus.Inactive;
    }

    public void Archive()
    {
        Status = ProductStatus.Archived;
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("Product name is required.");
        }
    }

    private static void ValidatePrice(decimal price)
    {
        if (price <= 0)
        {
            throw new InvalidOperationException("Product price must be greater than zero.");
        }
    }
}
