namespace OrderService.API.Domain.Entities;

public class Shipment
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public Guid SellerId { get; set; }
    public string Status { get; set; } = "Created";
    public string? TrackingNumber { get; set; }
}
