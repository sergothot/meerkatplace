using UserService.API.Application.Abstractions;
using UserService.API.Domain.Entities;
using UserService.API.Infrastructure.Persistence;

namespace UserService.API.Infrastructure.Repositories;

public sealed class BuyerProfileRepository(UserDbContext db) : IBuyerProfileRepository
{
    public async Task AddAsync(BuyerProfile profile, CancellationToken cancellationToken = default)
    {
        await db.BuyerProfiles.AddAsync(profile, cancellationToken);
    }
}
