namespace OrderService.API.Domain.Entities;
using Common.Shared.Domain.Enums;
public class CartItem
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public Currency Currency { get; set; }
}