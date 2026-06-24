using Common.Shared.Application.Interfaces;

namespace PaymentService.API.Infrastructure.Persistence;

public sealed class PaymentUnitOfWork(PaymentDbContext db) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return db.SaveChangesAsync(cancellationToken);
    }
}
