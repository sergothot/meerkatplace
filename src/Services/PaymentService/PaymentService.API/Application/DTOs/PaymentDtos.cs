namespace PaymentService.API.Application.DTOs;

public record CreatePaymentRequest(Guid OrderId, string Method, decimal Amount, string Currency);

public record PaymentResponse(Guid PaymentId, Guid OrderId, string Status, decimal Amount, string Currency, string Method, DateTimeOffset CreatedAt);

public record WalletDto(Guid UserId, decimal Balance, string Currency);

public record WalletTopUpRequest(decimal Amount, string Currency);

public record WalletWithdrawRequest(decimal Amount, string Currency);
