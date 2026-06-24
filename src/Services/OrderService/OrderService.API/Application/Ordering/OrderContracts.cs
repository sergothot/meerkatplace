namespace OrderService.API.Application.Ordering;

public record AddCartItemRequest(Guid ProductId, int Quantity, decimal UnitPrice, string Currency);

public record UpdateCartItemRequest(int Quantity);

public record CheckoutRequest(string AddressId, string PaymentMethod);

public record CartItemDto(Guid ItemId, Guid ProductId, int Quantity, decimal UnitPrice, decimal LineTotal, string Currency);

public record CartDto(Guid CartId, Guid BuyerId, IReadOnlyList<CartItemDto> Items, decimal Total, string Currency);

public record CheckoutResponse(Guid OrderId, string Status, decimal Amount, string Currency, bool RequiresPayment);

public record OrderSummaryDto(Guid OrderId, string Status, decimal Amount, string Currency, DateTimeOffset CreatedAt);

public record OrderDetailsDto(Guid OrderId, Guid BuyerId, string Status, decimal Amount, string Currency, DateTimeOffset CreatedAt, IReadOnlyList<CartItemDto> Items);

public record ShipmentDto(Guid ShipmentId, Guid OrderId, string Status, string? TrackingNumber);

public sealed class CartState
{
    public Guid CartId { get; init; } = Guid.NewGuid();
    public Guid BuyerId { get; init; }
    public List<CartItemState> Items { get; } = new();
}

public sealed class CartItemState
{
    public Guid ItemId { get; init; } = Guid.NewGuid();
    public Guid ProductId { get; init; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; init; }
    public string Currency { get; init; } = "RUB";
}

public sealed class OrderState
{
    public Guid OrderId { get; init; } = Guid.NewGuid();
    public Guid BuyerId { get; init; }
    public string Status { get; set; } = "Placed";
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "RUB";
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public List<CartItemDto> Items { get; init; } = new();
    public List<ShipmentDto> Shipments { get; init; } = new();
}