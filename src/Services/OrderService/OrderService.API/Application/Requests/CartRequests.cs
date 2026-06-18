using Common.Shared.Domain.Enums;

namespace OrderService.API.Application.Requests;

public sealed record AddCartItemRequest(Guid ProductId, int Quantity, decimal UnitPrice, string Currency);

public sealed record UpdateCartItemRequest(int Quantity);

public sealed record CheckoutRequest(Guid AddressId, string PaymentMethod);