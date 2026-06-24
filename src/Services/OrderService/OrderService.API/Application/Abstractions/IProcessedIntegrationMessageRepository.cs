using OrderService.API.Domain.Entities;

namespace OrderService.API.Application.Abstractions;

public interface IProcessedIntegrationMessageRepository
{
    Task<bool> ExistsAsync(string messageId, CancellationToken cancellationToken = default);

    Task AddAsync(ProcessedIntegrationMessage message, CancellationToken cancellationToken = default);
}