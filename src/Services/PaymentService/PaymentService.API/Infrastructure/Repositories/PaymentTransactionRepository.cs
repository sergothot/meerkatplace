using Microsoft.EntityFrameworkCore;
using PaymentService.API.Application.Abstractions;
using PaymentService.API.Domain.Entities;
using PaymentService.API.Infrastructure.Persistence;

namespace PaymentService.API.Infrastructure.Repositories;

public sealed class PaymentTransactionRepository(PaymentDbContext db) : IPaymentTransactionRepository
{
    public async Task<PaymentTransaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await db.PaymentTransactions
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<PaymentTransaction?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return await db.PaymentTransactions
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync(p => p.OrderId == orderId, cancellationToken);
    }

    public async Task AddAsync(PaymentTransaction payment, CancellationToken cancellationToken = default)
    {
        await db.PaymentTransactions.AddAsync(payment, cancellationToken);
    }
}
