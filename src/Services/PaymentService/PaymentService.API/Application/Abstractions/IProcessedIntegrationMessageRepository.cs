using PaymentService.API.Domain.Entities;

namespace PaymentService.API.Application.Abstractions;

public interface IProcessedIntegrationMessageRepository
{
    Task<bool> ExistsAsync(string messageId, CancellationToken cancellationToken = default);

    Task AddAsync(ProcessedIntegrationMessage message, CancellationToken cancellationToken = default);
}