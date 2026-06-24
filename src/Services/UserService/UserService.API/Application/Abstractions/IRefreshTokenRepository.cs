using UserService.API.Domain.Entities;

namespace UserService.API.Application.Abstractions;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);

    Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);

    void Remove(RefreshToken refreshToken);
}
