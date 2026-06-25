using System.Security.Claims;
using Common.Shared.Domain.Enums;
using Common.Shared.Application.Interfaces;
using Common.Shared.Application.IntegrationEvents;
using MassTransit;
using OrderService.API.Application.Abstractions;
using OrderService.API.Domain.Entities;
using OrderService.API.Domain.Enums;

namespace OrderService.API.Application.Ordering;

public sealed class DbCartService(
    ICartRepository carts,
    IOrderRepository orders,
    IUnitOfWork unitOfWork,
    IPublishEndpoint publishEndpoint,
    IProductCatalogClient productCatalogClient) : ICartService
{
    public async Task<IResult> GetCartAsync(HttpContext httpContext)
    {
        if (!TryResolveBuyerId(httpContext, out var buyerId))
        {
            return Results.Unauthorized();
        }

        var cart = await carts.GetByBuyerIdWithItemsAsync(buyerId);

        if (cart is null)
        {
            cart = new Cart { BuyerId = buyerId };
            await carts.AddAsync(cart);
            await unitOfWork.SaveChangesAsync();
        }

        return Results.Ok(ToCartDto(cart));
    }

    public async Task<IResult> AddCartItemAsync(HttpContext httpContext, AddCartItemRequest request)
    {
        var errors = OrderRequestValidator.ValidateAddCartItem(request);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var pricing = await productCatalogClient.GetProductPricingAsync(request.ProductId);
        if (pricing is null)
        {
            return Results.NotFound(new
            {
                error = new
                {
                    code = "PRODUCT_NOT_FOUND",
                    message = "Product not found in catalog."
                }
            });
        }

        if (!TryParseCurrency(pricing.Currency, out var currency))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["currency"] = ["Currency is not supported."]
            });
        }

        if (!TryResolveBuyerId(httpContext, out var buyerId))
        {
            return Results.Unauthorized();
        }

        var cart = await carts.GetByBuyerIdAsync(buyerId);

        if (cart is null)
        {
            cart = new Cart { BuyerId = buyerId };
            await carts.AddAsync(cart);
            await unitOfWork.SaveChangesAsync();
        }

        try
        {
            cart.AddItem(request.ProductId, request.Quantity, pricing.Price, currency);
        }
        catch (InvalidOperationException ex)
        {
            return Results.UnprocessableEntity(new
            {
                error = new
                {
                    code = "INVALID_CART_ITEM",
                    message = ex.Message
                }
            });
        }

        var item = cart.Items.Last();
        item.CartId = cart.Id;
        await carts.AddItemAsync(item);

        await unitOfWork.SaveChangesAsync();

        var updatedCart = await carts.GetByBuyerIdWithItemsAsync(buyerId);
        return Results.Ok(ToCartDto(updatedCart!));
    }

    public async Task<IResult> UpdateCartItemAsync(HttpContext httpContext, Guid itemId, UpdateCartItemRequest request)
    {
        var errors = OrderRequestValidator.ValidateUpdateCartItem(request);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        if (!TryResolveBuyerId(httpContext, out var buyerId))
        {
            return Results.Unauthorized();
        }

        var cart = await carts.GetByBuyerIdWithItemsAsync(buyerId);

        if (cart is null)
        {
            return Results.NotFound();
        }

        var updated = cart.TryUpdateItemQuantity(itemId, request.Quantity);
        if (!updated)
        {
            return Results.NotFound();
        }

        await unitOfWork.SaveChangesAsync();
        return Results.Ok(ToCartDto(cart));
    }

    public async Task<IResult> DeleteCartItemAsync(HttpContext httpContext, Guid itemId)
    {
        if (!TryResolveBuyerId(httpContext, out var buyerId))
        {
            return Results.Unauthorized();
        }

        var cart = await carts.GetByBuyerIdWithItemsAsync(buyerId);

        if (cart is null)
        {
            return Results.NotFound();
        }

        var item = cart.Items.FirstOrDefault(i => i.Id == itemId);
        if (item is null)
        {
            return Results.NotFound();
        }

        cart.TryRemoveItem(itemId);
        carts.RemoveItem(item);

        await unitOfWork.SaveChangesAsync();
        return Results.NoContent();
    }

    public async Task<IResult> CheckoutAsync(HttpContext httpContext, CheckoutRequest request)
    {
        var errors = OrderRequestValidator.ValidateCheckout(request);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        if (!TryResolveBuyerId(httpContext, out var buyerId))
        {
            return Results.Unauthorized();
        }

        var cart = await carts.GetByBuyerIdWithItemsAsync(buyerId);

        if (cart is null || cart.IsEmpty())
        {
            return Results.UnprocessableEntity(new
            {
                error = new
                {
                    code = "EMPTY_CART",
                    message = "Cannot checkout an empty cart."
                }
            });
        }

        var currency = cart.TryGetSingleCurrency();
        if (currency is null)
        {
            return Results.UnprocessableEntity(new
            {
                error = new
                {
                    code = "MULTI_CURRENCY_CART",
                    message = "Cart must contain a single currency before checkout."
                }
            });
        }

        var checkoutItems = cart.Items.ToList();

        var orderItems = checkoutItems
            .Select(i => OrderItem.Create(i.ProductId, Guid.Empty, i.Quantity, i.UnitPrice, i.Currency))
            .ToList();

        Order order;
        try
        {
            order = Order.CreatePlaced(buyerId, currency.Value, orderItems);
        }
        catch (InvalidOperationException ex)
        {
            return Results.UnprocessableEntity(new
            {
                error = new
                {
                    code = "INVALID_ORDER",
                    message = ex.Message
                }
            });
        }

        await orders.AddAsync(order);
        var eventItems = checkoutItems.Select(i => new CheckoutItem(i.ProductId, i.Quantity)).ToList();
        var correlationId = Guid.NewGuid();

        await publishEndpoint.Publish(new CheckoutRequested(
            correlationId,
            order.Id,
            order.BuyerId,
            order.TotalAmount,
            order.Currency.ToString(),
            request.PaymentMethod,
            eventItems));

        carts.Remove(cart);
        await unitOfWork.SaveChangesAsync();

        return Results.Created($"/orders/{order.Id}", new CheckoutResponse(
            order.Id,
            order.Status.ToString(),
            order.TotalAmount,
            order.Currency.ToString(),
            RequiresPayment: true));
    }

    private static bool TryResolveBuyerId(HttpContext httpContext, out Guid buyerId)
    {
        var claim = httpContext.User.FindFirstValue("sub")
            ?? httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(claim, out buyerId);
    }

    private static bool TryParseCurrency(string value, out Currency currency)
    {
        return Enum.TryParse(value, true, out currency);
    }

    private static CartDto ToCartDto(Cart cart)
    {
        var items = cart.Items.Select(i => new CartItemDto(
            i.Id,
            i.ProductId,
            i.Quantity,
            i.UnitPrice,
            i.Quantity * i.UnitPrice,
            i.Currency.ToString())).ToList();

        var currency = items.Select(i => i.Currency).FirstOrDefault() ?? Currency.RUB.ToString();
        var total = items.Sum(i => i.LineTotal);

        return new CartDto(cart.Id, cart.BuyerId, items, total, currency);
    }
}
