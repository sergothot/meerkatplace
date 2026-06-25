using Common.Shared.Application.IntegrationEvents;
using Common.Shared.Application.Interfaces;
using Common.Shared.Domain.Enums;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using PaymentService.API.Application.Abstractions;
using PaymentService.API.Domain.Entities;
using PaymentService.API.Domain.Enums;

namespace PaymentService.API.Integration.Consumers;

public sealed class PaymentRequestedConsumer(
    IPaymentTransactionRepository payments,
    IWalletRepository wallets,
    IProcessedIntegrationMessageRepository processedMessages,
    IUnitOfWork unitOfWork,
    IPublishEndpoint publishEndpoint) : IConsumer<PaymentRequested>
{
    public async Task Consume(ConsumeContext<PaymentRequested> context)
    {
        var message = context.Message;
        var messageId = context.MessageId?.ToString() ??
            $"PaymentRequested:{message.CorrelationId}:{message.OrderId}";

        if (await processedMessages.ExistsAsync(messageId, context.CancellationToken))
        {
            return;
        }

        var existing = await payments.GetByOrderIdAsync(message.OrderId);
        if (existing is not null)
        {
            await PublishResultForExistingPayment(message, existing);

            try
            {
                await MarkMessageProcessedAsync(messageId, context.CancellationToken);
            }
            catch (DbUpdateException)
            {
                return;
            }

            return;
        }

        if (!Enum.TryParse(message.Currency, true, out Currency currency) ||
            !Enum.TryParse(message.Method, true, out PaymentMethod method))
        {
            await publishEndpoint.Publish(new PaymentFailed(message.CorrelationId, message.OrderId, "Unsupported payment method or currency."));

            try
            {
                await MarkMessageProcessedAsync(messageId, context.CancellationToken);
            }
            catch (DbUpdateException)
            {
                return;
            }

            return;
        }

        PaymentTransaction payment;
        try
        {
            payment = PaymentTransaction.CreatePending(message.OrderId, message.Amount, currency, method);
        }
        catch (InvalidOperationException)
        {
            await publishEndpoint.Publish(new PaymentFailed(message.CorrelationId, message.OrderId, "Invalid payment payload."));

            try
            {
                await MarkMessageProcessedAsync(messageId, context.CancellationToken);
            }
            catch (DbUpdateException)
            {
                return;
            }

            return;
        }

        if (method == PaymentMethod.Wallet)
        {
            var wallet = await wallets.GetByUserIdAsync(message.BuyerId);
            if (wallet is null)
            {
                wallet = Wallet.Create(message.BuyerId, currency);
                await wallets.AddAsync(wallet);
            }

            if (wallet.TryDebit(message.Amount, currency))
            {
                payment.MarkSucceeded();
            }
            else
            {
                payment.MarkFailed();
            }
        }
        else
        {
            payment.MarkSucceeded();
        }

        await payments.AddAsync(payment, context.CancellationToken);

        if (payment.Status == PaymentStatus.Succeeded)
        {
            await publishEndpoint.Publish(new PaymentSucceeded(message.CorrelationId, message.OrderId, payment.Id));
        }
        else
        {
            await publishEndpoint.Publish(new PaymentFailed(message.CorrelationId, message.OrderId, "Payment was not successful."));
        }

        try
        {
            await MarkMessageProcessedAsync(messageId, context.CancellationToken);
        }
        catch (DbUpdateException)
        {
            return;
        }
    }

    private async Task MarkMessageProcessedAsync(string messageId, CancellationToken cancellationToken)
    {
        await processedMessages.AddAsync(new ProcessedIntegrationMessage
        {
            MessageId = messageId,
            Consumer = nameof(PaymentRequestedConsumer)
        }, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task PublishResultForExistingPayment(PaymentRequested message, PaymentTransaction existing)
    {
        if (existing.Status == PaymentStatus.Succeeded)
        {
            await publishEndpoint.Publish(new PaymentSucceeded(message.CorrelationId, message.OrderId, existing.Id));
            return;
        }

        if (existing.Status == PaymentStatus.Failed)
        {
            await publishEndpoint.Publish(new PaymentFailed(message.CorrelationId, message.OrderId, "Payment already failed."));
            return;
        }

        await publishEndpoint.Publish(new PaymentFailed(message.CorrelationId, message.OrderId, "Payment processing is not finalized."));
    }
}
