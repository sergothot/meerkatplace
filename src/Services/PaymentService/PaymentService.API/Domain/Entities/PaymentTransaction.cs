using Common.Shared.Domain.Enums;
using PaymentService.API.Domain.Enums;

namespace PaymentService.API.Domain.Entities;

public class PaymentTransaction
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OrderId { get; private set; }
    public decimal Amount { get; private set; }
    public Currency Currency { get; private set; }
    public PaymentMethod PaymentMethod { get; private set; }
    public PaymentStatus Status { get; private set; } = PaymentStatus.Pending;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    public static PaymentTransaction CreatePending(Guid orderId, decimal amount, Currency currency, PaymentMethod method)
    {
        if (orderId == Guid.Empty)
        {
            throw new InvalidOperationException("OrderId is required.");
        }

        if (amount <= 0)
        {
            throw new InvalidOperationException("Payment amount must be greater than zero.");
        }

        return new PaymentTransaction
        {
            OrderId = orderId,
            Amount = amount,
            Currency = currency,
            PaymentMethod = method,
            Status = PaymentStatus.Pending
        };
    }

    public void MarkSucceeded()
    {
        if (Status != PaymentStatus.Pending)
        {
            throw new InvalidOperationException("Only pending payments can be marked succeeded.");
        }

        Status = PaymentStatus.Succeeded;
    }

    public void MarkFailed()
    {
        if (Status != PaymentStatus.Pending)
        {
            throw new InvalidOperationException("Only pending payments can be marked failed.");
        }

        Status = PaymentStatus.Failed;
    }

    public void MarkRefunded()
    {
        if (Status != PaymentStatus.Succeeded)
        {
            throw new InvalidOperationException("Only succeeded payments can be refunded.");
        }

        Status = PaymentStatus.Refunded;
    }
}
