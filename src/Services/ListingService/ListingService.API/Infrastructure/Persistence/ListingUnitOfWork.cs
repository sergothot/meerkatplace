using Common.Shared.Application.Interfaces;

namespace ListingService.API.Infrastructure.Persistence;

public sealed class ListingUnitOfWork(ListingDbContext db) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return db.SaveChangesAsync(cancellationToken);
    }
}
