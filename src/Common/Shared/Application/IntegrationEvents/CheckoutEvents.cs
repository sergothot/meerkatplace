namespace Common.Shared.Application.IntegrationEvents;

public record CheckoutItem(Guid ProductId, int Quantity);

public record CheckoutRequested(
    Guid CorrelationId,
    Guid OrderId,
    Guid BuyerId,
    decimal Amount,
    string Currency,
    string PaymentMethod,
    IReadOnlyList<CheckoutItem> Items);

public record StockReserved(
    Guid CorrelationId,
    Guid OrderId,
    Guid BuyerId,
    decimal Amount,
    string Currency,
    string PaymentMethod);

public record StockReservationFailed(Guid CorrelationId, Guid OrderId, string Reason);

public record PaymentRequested(
    Guid CorrelationId,
    Guid OrderId,
    Guid BuyerId,
    decimal Amount,
    string Currency,
    string Method);

public record PaymentSucceeded(Guid CorrelationId, Guid OrderId, Guid PaymentId);

public record PaymentFailed(Guid CorrelationId, Guid OrderId, string Reason);

public record ReleaseStockRequested(Guid CorrelationId, Guid OrderId, IReadOnlyList<CheckoutItem> Items);