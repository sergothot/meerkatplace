using Common.Shared.Application.Interfaces;

namespace UserService.API.Infrastructure.Persistence;

public sealed class UserUnitOfWork(UserDbContext db) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return db.SaveChangesAsync(cancellationToken);
    }
}
