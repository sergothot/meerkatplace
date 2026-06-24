namespace OrderService.API.Application.DTOs;

public record AddCartItemRequest(Guid ProductId, int Quantity, decimal UnitPrice, string Currency);

public record UpdateCartItemRequest(int Quantity);

public record CheckoutRequest(string AddressId, string PaymentMethod);

public record CartItemDto(Guid ItemId, Guid ProductId, int Quantity, decimal UnitPrice, decimal LineTotal, string Currency);

public record CartDto(Guid CartId, Guid BuyerId, IReadOnlyList<CartItemDto> Items, decimal Total, string Currency);

public record CheckoutResponse(Guid OrderId, string Status, decimal Amount, string Currency, bool RequiresPayment);

public record OrderSummaryDto(Guid OrderId, string Status, decimal Amount, string Currency, DateTimeOffset CreatedAt);

public record OrderDetailsDto(Guid OrderId, Guid BuyerId, string Status, decimal Amount, string Currency, DateTimeOffset CreatedAt, IReadOnlyList<CartItemDto> Items);

public record ShipmentDto(Guid ShipmentId, Guid OrderId, string Status, string? TrackingNumber);
