using Common.Shared.Domain.Enums;

namespace PaymentService.API.Domain.Entities;

public class Wallet
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public decimal Balance { get; private set; }
    public Currency Currency { get; private set; }

    public static Wallet Create(Guid userId, Currency currency, decimal initialBalance = 10000m)
    {
        if (initialBalance < 0)
        {
            throw new InvalidOperationException("Initial wallet balance cannot be negative.");
        }

        return new Wallet
        {
            UserId = userId,
            Currency = currency,
            Balance = initialBalance
        };
    }

    public bool TryDebit(decimal amount, Currency currency)
    {
        if (amount <= 0)
        {
            throw new InvalidOperationException("Amount must be greater than zero.");
        }

        if (Currency != currency || Balance < amount)
        {
            return false;
        }

        Balance -= amount;
        return true;
    }

    public void Credit(decimal amount, Currency currency)
    {
        if (amount <= 0)
        {
            throw new InvalidOperationException("Amount must be greater than zero.");
        }

        if (Currency != currency)
        {
            throw new InvalidOperationException("Wallet currency mismatch.");
        }

        Balance += amount;
    }

    public void Withdraw(decimal amount, Currency currency)
    {
        if (amount <= 0)
        {
            throw new InvalidOperationException("Amount must be greater than zero.");
        }

        if (Currency != currency)
        {
            throw new InvalidOperationException("Wallet currency mismatch.");
        }

        if (Balance < amount)
        {
            throw new InvalidOperationException("Insufficient wallet balance.");
        }

        Balance -= amount;
    }
}
