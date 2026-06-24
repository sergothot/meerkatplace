using Common.Shared.Domain.Enums;
using ListingService.API.Domain.Entities;
using ListingService.API.Domain.Enums;

namespace ListingService.UnitTests;

public class ProductDomainTests
{
    [Fact]
    public void Create_WithInvalidName_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            Product.Create(Guid.NewGuid(), " ", "desc", 10m, Currency.RUB, DeliveryType.Physical));
    }

    [Fact]
    public void Create_WithInvalidPrice_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            Product.Create(Guid.NewGuid(), "Book", "desc", 0m, Currency.RUB, DeliveryType.Physical));
    }

    [Fact]
    public void Activate_FromDraft_SetsActiveStatus()
    {
        var product = Product.Create(Guid.NewGuid(), "Book", "desc", 100m, Currency.RUB, DeliveryType.Physical);

        product.Activate();

        Assert.Equal(ProductStatus.Active, product.Status);
    }

    [Fact]
    public void Activate_AfterArchive_Throws()
    {
        var product = Product.Create(Guid.NewGuid(), "Book", "desc", 100m, Currency.RUB, DeliveryType.Physical);
        product.Archive();

        Assert.Throws<InvalidOperationException>(product.Activate);
    }
}
