using Common.Shared.Domain.Enums;
using OrderService.API.Domain.Entities;
using OrderService.API.Domain.Enums;

namespace OrderService.UnitTests;

public class OrderItemDomainTests
{
    [Fact]
    public void Create_WithInvalidQuantity_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            OrderItem.Create(Guid.NewGuid(), Guid.NewGuid(), 0, 10m, Currency.RUB));
    }

    [Fact]
    public void Create_WithInvalidUnitPrice_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            OrderItem.Create(Guid.NewGuid(), Guid.NewGuid(), 1, 0m, Currency.RUB));
    }

    [Fact]
    public void MarkDelivered_FromPending_Succeeds()
    {
        var item = OrderItem.Create(Guid.NewGuid(), Guid.NewGuid(), 1, 10m, Currency.RUB);

        item.MarkDelivered();

        Assert.Equal(FulfillmentStatus.Delivered, item.FulfillmentStatus);
    }

    [Fact]
    public void MarkDelivered_WhenAlreadyDelivered_Throws()
    {
        var item = OrderItem.Create(Guid.NewGuid(), Guid.NewGuid(), 1, 10m, Currency.RUB);
        item.MarkDelivered();

        Assert.Throws<InvalidOperationException>(item.MarkDelivered);
    }
}

public class ShipmentDomainTests
{
    [Fact]
    public void Create_InitializesCreatedStatus()
    {
        var shipment = Shipment.Create(Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal(ShipmentStatus.Created, shipment.ShipmentStatus);
        Assert.Null(shipment.TrackingNumber);
    }

    [Fact]
    public void MarkInTransit_WithTrackingNumber_Succeeds()
    {
        var shipment = Shipment.Create(Guid.NewGuid(), Guid.NewGuid());

        shipment.MarkInTransit("TRK-123");

        Assert.Equal(ShipmentStatus.InTransit, shipment.ShipmentStatus);
        Assert.Equal("TRK-123", shipment.TrackingNumber);
    }

    [Fact]
    public void MarkDelivered_FromCreated_Throws()
    {
        var shipment = Shipment.Create(Guid.NewGuid(), Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(shipment.MarkDelivered);
    }

    [Fact]
    public void Cancel_AfterDelivered_Throws()
    {
        var shipment = Shipment.Create(Guid.NewGuid(), Guid.NewGuid());
        shipment.MarkInTransit("TRK-123");
        shipment.MarkDelivered();

        Assert.Throws<InvalidOperationException>(shipment.Cancel);
    }
}
