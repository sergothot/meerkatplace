using Common.Shared.Application.IntegrationEvents;
using Common.Shared.Application.Interfaces;
using ListingService.API.Application.Abstractions;
using ListingService.API.Domain.Enums;
using MassTransit;

namespace ListingService.API.Integration.Consumers;

public sealed class ReleaseStockRequestedConsumer(
    IInventoryStockRepository stocks,
    IStockReservationRepository reservations,
    IUnitOfWork unitOfWork) : IConsumer<ReleaseStockRequested>
{
    public async Task Consume(ConsumeContext<ReleaseStockRequested> context)
    {
        var reservation = await reservations.GetByOrderIdAsync(context.Message.OrderId);
        if (reservation is null || reservation.Status != StockReservationStatus.Reserved)
        {
            return;
        }

        foreach (var item in context.Message.Items)
        {
            var stock = await stocks.GetByProductIdAsync(item.ProductId);
            if (stock is null)
            {
                continue;
            }

            stock.Release(item.Quantity);
            stocks.Update(stock);
        }

        reservation.MarkReleased();
        reservations.Update(reservation);

        await unitOfWork.SaveChangesAsync();
    }
}
