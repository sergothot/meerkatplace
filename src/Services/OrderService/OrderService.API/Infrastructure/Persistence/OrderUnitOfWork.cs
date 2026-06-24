using Common.Shared.Application.Interfaces;

namespace OrderService.API.Infrastructure.Persistence;

public sealed class OrderUnitOfWork(OrderDbContext db) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return db.SaveChangesAsync(cancellationToken);
    }
}
