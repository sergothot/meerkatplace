using UserService.API.Domain.Entities;

namespace UserService.API.Application.Abstractions;

public interface IUserRepository
{
    Task<bool> AnyByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<bool> AnyByLoginAsync(string login, Guid? excludeUserId = null, CancellationToken cancellationToken = default);

    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task AddAsync(User user, CancellationToken cancellationToken = default);
}
