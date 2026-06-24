using Microsoft.EntityFrameworkCore;
using PaymentService.API.Application.Abstractions;
using PaymentService.API.Domain.Entities;
using PaymentService.API.Infrastructure.Persistence;

namespace PaymentService.API.Infrastructure.Repositories;

public sealed class ProcessedIntegrationMessageRepository(PaymentDbContext db) : IProcessedIntegrationMessageRepository
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