using Microsoft.EntityFrameworkCore;
using UserService.API.Application.Abstractions;
using UserService.API.Domain.Entities;
using UserService.API.Infrastructure.Persistence;

namespace UserService.API.Infrastructure.Repositories;

public sealed class AddressRepository(UserDbContext db) : IAddressRepository
{
    public async Task<IReadOnlyList<Address>> ListByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await db.Addresses
            .Where(a => a.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public async Task<Address?> GetByIdForUserAsync(Guid addressId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await db.Addresses
            .FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId, cancellationToken);
    }

    public async Task AddAsync(Address address, CancellationToken cancellationToken = default)
    {
        await db.Addresses.AddAsync(address, cancellationToken);
    }

    public void Remove(Address address)
    {
        db.Addresses.Remove(address);
    }
}
