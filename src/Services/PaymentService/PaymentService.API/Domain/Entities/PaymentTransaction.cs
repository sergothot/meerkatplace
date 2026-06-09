namespace PaymentService.API.Domain.Entities;

public class PaymentTransaction
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "RUB";
    public string Method { get; set; } = "Wallet";
    public string Status { get; set; } = "Pending";
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
