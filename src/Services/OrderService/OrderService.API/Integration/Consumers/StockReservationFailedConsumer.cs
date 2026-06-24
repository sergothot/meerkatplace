using Common.Shared.Application.IntegrationEvents;
using Common.Shared.Application.Interfaces;
using MassTransit;
using OrderService.API.Application.Abstractions;
using OrderService.API.Domain.Entities;

namespace OrderService.API.Integration.Consumers;

public sealed class StockReservationFailedConsumer(
    IOrderRepository orders,
    IProcessedIntegrationMessageRepository processedMessages,
    IUnitOfWork unitOfWork) : IConsumer<StockReservationFailed>
{
    public async Task Consume(ConsumeContext<StockReservationFailed> context)
    {
        var message = context.Message;
        var messageId = context.MessageId?.ToString() ??
            $"StockReservationFailed:{message.CorrelationId}:{message.OrderId}";

        if (await processedMessages.ExistsAsync(messageId, context.CancellationToken))
        {
            return;
        }

        var order = await orders.GetByIdAsync(message.OrderId);
        if (order is null)
        {
            await MarkProcessedAsync(messageId, context.CancellationToken);
            return;
        }

        try
        {
            order.MarkPaymentFailed();
        }
        catch (InvalidOperationException)
        {
            await MarkProcessedAsync(messageId, context.CancellationToken);
            return;
        }

        orders.Update(order);
        await MarkProcessedAsync(messageId, context.CancellationToken);
    }

    private async Task MarkProcessedAsync(string messageId, CancellationToken cancellationToken)
    {
        await processedMessages.AddAsync(new ProcessedIntegrationMessage
        {
            MessageId = messageId,
            Consumer = nameof(StockReservationFailedConsumer)
        }, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
