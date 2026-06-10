namespace PaymentService.API.Application.Payments;

public record CreatePaymentRequest(Guid OrderId, string Method, decimal Amount, string Currency);

public record PaymentResponse(Guid PaymentId, Guid OrderId, string Status, decimal Amount, string Currency, string Method, DateTimeOffset CreatedAt);

public record WalletDto(Guid UserId, decimal Balance, string Currency);

public record WalletTopUpRequest(decimal Amount, string Currency);

public record WalletWithdrawRequest(decimal Amount, string Currency);

public sealed class PaymentState
{
    public Guid PaymentId { get; init; } = Guid.NewGuid();
    public Guid OrderId { get; init; }
    public string Status { get; set; } = "Pending";
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "RUB";
    public string Method { get; init; } = "Wallet";
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
