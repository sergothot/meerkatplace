using Common.Shared.Application.IntegrationEvents;
using Common.Shared.Application.Interfaces;
using ListingService.API.Application.Abstractions;
using ListingService.API.Domain.Entities;
using ListingService.API.Domain.Enums;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace ListingService.API.Integration.Consumers;

public sealed class CheckoutRequestedConsumer(
    IInventoryStockRepository stocks,
    IStockReservationRepository reservations,
    IUnitOfWork unitOfWork,
    IPublishEndpoint publishEndpoint) : IConsumer<CheckoutRequested>
{
    public async Task Consume(ConsumeContext<CheckoutRequested> context)
    {
        var message = context.Message;

        var existingReservation = await reservations.GetByOrderIdAsync(message.OrderId);
        if (existingReservation is not null)
        {
            await PublishExistingReservationResult(existingReservation, message);
            await unitOfWork.SaveChangesAsync();
            return;
        }

        var stockMap = new Dictionary<Guid, InventoryStock>();
        foreach (var item in message.Items)
        {
            var stock = await stocks.GetByProductIdAsync(item.ProductId);
            if (stock is null || !stock.TryReserve(item.Quantity))
            {
                var reason = $"Insufficient stock for product {item.ProductId}.";
                await PersistAndPublishFailure(message, reason);
                return;
            }

            stockMap[item.ProductId] = stock;
        }

        foreach (var stock in stockMap.Values)
        {
            stocks.Update(stock);
        }

        await reservations.AddAsync(StockReservation.CreateReserved(message.OrderId));

        await publishEndpoint.Publish(new StockReserved(
            message.CorrelationId,
            message.OrderId,
            message.BuyerId,
            message.Amount,
            message.Currency,
            message.PaymentMethod));

        try
        {
            await unitOfWork.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            var persisted = await reservations.GetByOrderIdAsync(message.OrderId);
            if (persisted is not null)
            {
                await PublishExistingReservationResult(persisted, message);
                await unitOfWork.SaveChangesAsync();
            }

            return;
        }
    }

    private async Task PersistAndPublishFailure(CheckoutRequested message, string reason)
    {
        await reservations.AddAsync(StockReservation.CreateFailed(message.OrderId, reason));

        await publishEndpoint.Publish(new StockReservationFailed(
            message.CorrelationId,
            message.OrderId,
            reason));

        try
        {
            await unitOfWork.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            var persisted = await reservations.GetByOrderIdAsync(message.OrderId);
            if (persisted is not null)
            {
                await PublishExistingReservationResult(persisted, message);
                await unitOfWork.SaveChangesAsync();
                return;
            }
        }
    }

    private async Task PublishExistingReservationResult(StockReservation reservation, CheckoutRequested message)
    {
        if (reservation.Status == StockReservationStatus.Reserved)
        {
            await publishEndpoint.Publish(new StockReserved(
                message.CorrelationId,
                message.OrderId,
                message.BuyerId,
                message.Amount,
                message.Currency,
                message.PaymentMethod));
            return;
        }

        var reason = reservation.FailureReason;
        if (string.IsNullOrWhiteSpace(reason))
        {
            reason = reservation.Status == StockReservationStatus.Released
                ? "Stock reservation was already released for this order."
                : "Stock reservation has already failed for this order.";
        }

        await publishEndpoint.Publish(new StockReservationFailed(
            message.CorrelationId,
            message.OrderId,
            reason));
    }
}
