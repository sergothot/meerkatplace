using OrderService.API.Domain.Enums;

namespace OrderService.API.Domain.Entities;

public class Shipment
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OrderId { get; private set; }
    public Guid SellerId { get; private set; }
    public ShipmentStatus ShipmentStatus { get; private set; } = ShipmentStatus.Created;
    public string? TrackingNumber { get; private set; }

    public static Shipment Create(Guid orderId, Guid sellerId)
    {
        return new Shipment
        {
            OrderId = orderId,
            SellerId = sellerId,
            ShipmentStatus = ShipmentStatus.Created
        };
    }

    public void MarkInTransit(string trackingNumber)
    {
        if (ShipmentStatus != ShipmentStatus.Created)
        {
            throw new InvalidOperationException("Only newly created shipments can move to in-transit.");
        }

        if (string.IsNullOrWhiteSpace(trackingNumber))
        {
            throw new InvalidOperationException("Tracking number is required for in-transit shipment.");
        }

        TrackingNumber = trackingNumber;
        ShipmentStatus = ShipmentStatus.InTransit;
    }

    public void MarkDelivered()
    {
        if (ShipmentStatus != ShipmentStatus.InTransit)
        {
            throw new InvalidOperationException("Only in-transit shipments can be delivered.");
        }

        ShipmentStatus = ShipmentStatus.Delivered;
    }

    public void Cancel()
    {
        if (ShipmentStatus == ShipmentStatus.Delivered)
        {
            throw new InvalidOperationException("Delivered shipments cannot be cancelled.");
        }

        ShipmentStatus = ShipmentStatus.Cancelled;
    }
}
