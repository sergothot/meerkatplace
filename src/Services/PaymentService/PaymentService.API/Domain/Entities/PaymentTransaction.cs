using Common.Shared.Domain.Enums;
using PaymentService.API.Domain.Enums;

namespace PaymentService.API.Domain.Entities;

public class PaymentTransaction
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
    public Currency Currency { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

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
