using Microsoft.EntityFrameworkCore;
using UserService.API.Application.Abstractions;
using UserService.API.Domain.Entities;
using UserService.API.Infrastructure.Persistence;

namespace UserService.API.Infrastructure.Repositories;

public sealed class RefreshTokenRepository(UserDbContext db) : IRefreshTokenRepository
{
    public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await db.RefreshTokens.FirstOrDefaultAsync(t => t.Token == token, cancellationToken);
    }

    public async Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        await db.RefreshTokens.AddAsync(refreshToken, cancellationToken);
    }

    public void Remove(RefreshToken refreshToken)
    {
        db.RefreshTokens.Remove(refreshToken);
    }
}
