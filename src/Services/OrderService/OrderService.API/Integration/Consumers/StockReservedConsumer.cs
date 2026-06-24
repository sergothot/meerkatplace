using Common.Shared.Application.IntegrationEvents;
using Common.Shared.Application.Interfaces;
using MassTransit;
using OrderService.API.Application.Abstractions;
using OrderService.API.Domain.Entities;

namespace OrderService.API.Integration.Consumers;

public sealed class StockReservedConsumer(
    IProcessedIntegrationMessageRepository processedMessages,
    IUnitOfWork unitOfWork,
    IPublishEndpoint publishEndpoint) : IConsumer<StockReserved>
{
    public async Task Consume(ConsumeContext<StockReserved> context)
    {
        var message = context.Message;
        var messageId = context.MessageId?.ToString() ??
            $"StockReserved:{message.CorrelationId}:{message.OrderId}";

        if (await processedMessages.ExistsAsync(messageId, context.CancellationToken))
        {
            return;
        }

        await publishEndpoint.Publish(new PaymentRequested(
            message.CorrelationId,
            message.OrderId,
            message.BuyerId,
            message.Amount,
            message.Currency,
            message.PaymentMethod));

        await processedMessages.AddAsync(new ProcessedIntegrationMessage
        {
            MessageId = messageId,
            Consumer = nameof(StockReservedConsumer)
        }, context.CancellationToken);

        await unitOfWork.SaveChangesAsync(context.CancellationToken);
    }
}
