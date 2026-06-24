using Microsoft.EntityFrameworkCore;
using UserService.API.Application.Abstractions;
using UserService.API.Domain.Entities;
using UserService.API.Infrastructure.Persistence;

namespace UserService.API.Infrastructure.Repositories;

public sealed class UserRepository(UserDbContext db) : IUserRepository
{
    public async Task<bool> AnyByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await db.Users.AnyAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<bool> AnyByLoginAsync(string login, Guid? excludeUserId = null, CancellationToken cancellationToken = default)
    {
        return await db.Users.AnyAsync(u => u.Login == login && (!excludeUserId.HasValue || u.Id != excludeUserId.Value), cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await db.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await db.Users.AddAsync(user, cancellationToken);
    }
}
