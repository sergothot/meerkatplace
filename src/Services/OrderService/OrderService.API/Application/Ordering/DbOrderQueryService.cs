using System.Security.Claims;
using Common.Shared.Application.IntegrationEvents;
using Common.Shared.Application.Interfaces;
using MassTransit;
using OrderService.API.Application.Abstractions;
using OrderService.API.Domain.Enums;

namespace OrderService.API.Application.Ordering;

public sealed class DbOrderQueryService(
    IOrderRepository orders,
    IShipmentRepository shipments,
    IUnitOfWork unitOfWork,
    IPublishEndpoint publishEndpoint) : IOrderQueryService
{
    public async Task<IResult> GetOrdersAsync(HttpContext httpContext)
    {
        if (!TryResolveBuyerId(httpContext, out var buyerId))
        {
            return Results.Unauthorized();
        }

        var buyerOrders = (await orders.ListByBuyerAsync(buyerId))
            .Select(o => new OrderSummaryDto(o.Id, o.Status.ToString(), o.TotalAmount, o.Currency.ToString(), o.CreatedAt))
            .ToList();

        return Results.Ok(buyerOrders);
    }

    public async Task<IResult> GetOrderAsync(HttpContext httpContext, Guid orderId)
    {
        if (!TryResolveBuyerId(httpContext, out var buyerId))
        {
            return Results.Unauthorized();
        }

        var order = await orders.GetByIdForBuyerAsync(orderId, buyerId, includeItems: true);

        if (order is null)
        {
            return Results.NotFound();
        }

        var items = order.Items.Select(i => new CartItemDto(
            i.Id,
            i.ProductId,
            i.Quantity,
            i.UnitPrice,
            i.Quantity * i.UnitPrice,
            i.Currency.ToString())).ToList();

        return Results.Ok(new OrderDetailsDto(
            order.Id,
            order.BuyerId,
            order.Status.ToString(),
            order.TotalAmount,
            order.Currency.ToString(),
            order.CreatedAt,
            items));
    }

    public async Task<IResult> GetOrderStatusAsync(HttpContext httpContext, Guid orderId)
    {
        if (!TryResolveBuyerId(httpContext, out var buyerId))
        {
            return Results.Unauthorized();
        }

        var order = await orders.GetByIdForBuyerAsync(orderId, buyerId, includeItems: true);

        if (order is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(new { orderId = order.Id, status = order.Status.ToString() });
    }

    public async Task<IResult> CancelOrderAsync(HttpContext httpContext, Guid orderId)
    {
        if (!TryResolveBuyerId(httpContext, out var buyerId))
        {
            return Results.Unauthorized();
        }

        var order = await orders.GetByIdForBuyerAsync(orderId, buyerId);

        if (order is null)
        {
            return Results.NotFound();
        }

        try
        {
            var wasPlaced = order.Status == OrderStatus.Placed;
            order.Cancel();

            if (wasPlaced)
            {
                var items = order.Items
                    .Select(i => new CheckoutItem(i.ProductId, i.Quantity))
                    .ToList();

                await publishEndpoint.Publish(new ReleaseStockRequested(
                    Guid.NewGuid(),
                    order.Id,
                    items));
            }
        }
        catch (InvalidOperationException ex)
        {
            return Results.UnprocessableEntity(new
            {
                error = new
                {
                    code = "INVALID_ORDER_TRANSITION",
                    message = ex.Message
                }
            });
        }

        await unitOfWork.SaveChangesAsync();

        return Results.Ok(new { orderId = order.Id, status = order.Status.ToString() });
    }

    public async Task<IResult> GetShipmentsAsync(HttpContext httpContext, Guid orderId)
    {
        if (!TryResolveBuyerId(httpContext, out var buyerId))
        {
            return Results.Unauthorized();
        }

        var ownsOrder = await orders.ExistsForBuyerAsync(orderId, buyerId);

        if (!ownsOrder)
        {
            return Results.NotFound();
        }

        var shipmentsForOrder = (await shipments.ListByOrderIdAsync(orderId))
            .Select(s => new ShipmentDto(s.Id, s.OrderId, s.ShipmentStatus.ToString(), s.TrackingNumber))
            .ToList();

        return Results.Ok(shipmentsForOrder);
    }

    private static bool TryResolveBuyerId(HttpContext httpContext, out Guid buyerId)
    {
        var claim = httpContext.User.FindFirstValue("sub")
            ?? httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(claim, out buyerId);
    }
}
