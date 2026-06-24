using Microsoft.EntityFrameworkCore;
using UserService.API.Application.Abstractions;
using UserService.API.Domain.Entities;
using UserService.API.Infrastructure.Persistence;

namespace UserService.API.Infrastructure.Repositories;

public sealed class SellerProfileRepository(UserDbContext db) : ISellerProfileRepository
{
    public async Task<bool> AnyByStoreNameAsync(string storeName, CancellationToken cancellationToken = default)
    {
        return await db.SellerProfiles.AnyAsync(s => s.StoreName == storeName, cancellationToken);
    }

    public async Task AddAsync(SellerProfile profile, CancellationToken cancellationToken = default)
    {
        await db.SellerProfiles.AddAsync(profile, cancellationToken);
    }
}
