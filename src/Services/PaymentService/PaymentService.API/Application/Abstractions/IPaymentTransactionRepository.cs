using PaymentService.API.Domain.Entities;

namespace PaymentService.API.Application.Abstractions;

public interface IPaymentTransactionRepository
{
    Task<PaymentTransaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PaymentTransaction?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);

    Task AddAsync(PaymentTransaction payment, CancellationToken cancellationToken = default);
}
