using Common.Shared.Domain.Enums;
using OrderService.API.Domain.Enums;
using OrderService.API.Application.DTOs;

namespace OrderService.API.Application.States;
public sealed class OrderState
{
    public Guid OrderId { get; init; } = Guid.NewGuid();
    public Guid BuyerId { get; init; }
    public OrderStatus OrderStatus { get; set; }
    public decimal Amount { get; init; }
    public Currency Currency { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public List<CartItemDTO> Items { get; init; } = new();
    public List<ShipmentDTO> Shipments { get; init; } = new();
}