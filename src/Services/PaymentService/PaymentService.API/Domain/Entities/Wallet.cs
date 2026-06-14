using Common.Shared.Domain.Enums;

namespace PaymentService.API.Domain.Entities;

public class Wallet
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public decimal Balance { get; set; }
    public Currency Currency { get; set; }
}
