namespace PaymentService.API.Domain.Entities;

public class Wallet
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public decimal Balance { get; set; }
    public string Currency { get; set; } = "RUB";
}
