namespace OrderService.API.Domain.Entities;
using OrderService.API.Domain.Enums;
public class Shipment
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public Guid SellerId { get; set; }
    public ShipmentStatus ShipmentStatus { get; set; } = ShipmentStatus.Created;
    public string? TrackingNumber { get; set; }
}
