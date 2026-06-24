using Common.Shared.Application.IntegrationEvents;
using Common.Shared.Application.Interfaces;
using MassTransit;
using OrderService.API.Application.Abstractions;
using OrderService.API.Domain.Entities;

namespace OrderService.API.Integration.Consumers;

public sealed class PaymentSucceededConsumer(
    IOrderRepository orders,
    IShipmentRepository shipments,
    IProcessedIntegrationMessageRepository processedMessages,
    IUnitOfWork unitOfWork) : IConsumer<PaymentSucceeded>
{
    public async Task Consume(ConsumeContext<PaymentSucceeded> context)
    {
        var message = context.Message;
        var messageId = context.MessageId?.ToString() ??
            $"PaymentSucceeded:{message.CorrelationId}:{message.OrderId}";

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

        var hasShipments = (await shipments.ListByOrderIdAsync(order.Id, context.CancellationToken)).Count > 0;

        try
        {
            order.MarkPaid();
        }
        catch (InvalidOperationException)
        {
            await MarkProcessedAsync(messageId, context.CancellationToken);
            return;
        }

        orders.Update(order);

        if (!hasShipments)
        {
            var sellerIds = order.Items
                .Select(i => i.SellerId)
                .Distinct()
                .ToList();

            if (sellerIds.Count == 0)
            {
                sellerIds.Add(Guid.Empty);
            }

            foreach (var sellerId in sellerIds)
            {
                await shipments.AddAsync(Shipment.Create(order.Id, sellerId), context.CancellationToken);
            }
        }

        await MarkProcessedAsync(messageId, context.CancellationToken);
    }

    private async Task MarkProcessedAsync(string messageId, CancellationToken cancellationToken)
    {
        await processedMessages.AddAsync(new ProcessedIntegrationMessage
        {
            MessageId = messageId,
            Consumer = nameof(PaymentSucceededConsumer)
        }, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
