namespace OrderService.API.Domain.Entities;
public class OrderItem
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    public Guid SellerId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}