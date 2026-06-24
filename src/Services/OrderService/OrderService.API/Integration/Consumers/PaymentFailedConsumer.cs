using Common.Shared.Application.IntegrationEvents;
using Common.Shared.Application.Interfaces;
using MassTransit;
using OrderService.API.Application.Abstractions;
using OrderService.API.Domain.Entities;

namespace OrderService.API.Integration.Consumers;

public sealed class PaymentFailedConsumer(
    IOrderRepository orders,
    IProcessedIntegrationMessageRepository processedMessages,
    IUnitOfWork unitOfWork,
    IPublishEndpoint publishEndpoint) : IConsumer<PaymentFailed>
{
    public async Task Consume(ConsumeContext<PaymentFailed> context)
    {
        var message = context.Message;
        var messageId = context.MessageId?.ToString() ??
            $"PaymentFailed:{message.CorrelationId}:{message.OrderId}";

        if (await processedMessages.ExistsAsync(messageId, context.CancellationToken))
        {
            return;
        }

        var order = await orders.GetByIdAsync(message.OrderId, includeItems: true);
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

        var items = order.Items
            .Select(i => new CheckoutItem(i.ProductId, i.Quantity))
            .ToList();

        await publishEndpoint.Publish(new ReleaseStockRequested(
            message.CorrelationId,
            order.Id,
            items));

        await MarkProcessedAsync(messageId, context.CancellationToken);
    }

    private async Task MarkProcessedAsync(string messageId, CancellationToken cancellationToken)
    {
        await processedMessages.AddAsync(new ProcessedIntegrationMessage
        {
            MessageId = messageId,
            Consumer = nameof(PaymentFailedConsumer)
        }, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
