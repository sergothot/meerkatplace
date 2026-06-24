using Microsoft.EntityFrameworkCore;
using OrderService.API.Application.Abstractions;
using OrderService.API.Domain.Entities;
using OrderService.API.Infrastructure.Persistence;

namespace OrderService.API.Infrastructure.Repositories;

public sealed class ProcessedIntegrationMessageRepository(OrderDbContext db) : IProcessedIntegrationMessageRepository
{
    public async Task<bool> ExistsAsync(string messageId, CancellationToken cancellationToken = default)
    {
        return await db.ProcessedIntegrationMessages
            .AnyAsync(x => x.MessageId == messageId, cancellationToken);
    }

    public async Task AddAsync(ProcessedIntegrationMessage message, CancellationToken cancellationToken = default)
    {
        await db.ProcessedIntegrationMessages.AddAsync(message, cancellationToken);
    }
}