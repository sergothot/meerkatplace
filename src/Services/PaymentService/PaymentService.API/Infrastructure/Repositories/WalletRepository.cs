using Microsoft.EntityFrameworkCore;
using PaymentService.API.Application.Abstractions;
using PaymentService.API.Domain.Entities;
using PaymentService.API.Infrastructure.Persistence;

namespace PaymentService.API.Infrastructure.Repositories;

public sealed class WalletRepository(PaymentDbContext db) : IWalletRepository
{
    public async Task<Wallet?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await db.Wallets
            .FirstOrDefaultAsync(w => w.UserId == userId, cancellationToken);
    }

    public async Task AddAsync(Wallet wallet, CancellationToken cancellationToken = default)
    {
        await db.Wallets.AddAsync(wallet, cancellationToken);
    }
}
