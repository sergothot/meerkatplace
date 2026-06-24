using PaymentService.API.Domain.Entities;

namespace PaymentService.API.Application.Abstractions;

public interface IWalletRepository
{
    Task<Wallet?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task AddAsync(Wallet wallet, CancellationToken cancellationToken = default);
}
