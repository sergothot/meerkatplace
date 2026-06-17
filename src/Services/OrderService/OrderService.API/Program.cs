using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using OrderService.API.Application.DTOs;
using OrderService.API.Infrastructure.Persistence;
using OrderService.API.Application.States;
using OrderService.API.Application.Requests;
using OrderService.API.Domain.Enums;
using OrderService.API.Domain.Entities;
using Common.Shared.Domain.Enums;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    await db.Database.EnsureCreatedAsync();
}

var carts = new ConcurrentDictionary<Guid, CartState>();
var orders = new ConcurrentDictionary<Guid, OrderState>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/", () => "Hello World!");
app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "order-service" }));
app.MapGet("/cart", (HttpContext httpContext) =>
{
    var buyerId = ResolveBuyerId(httpContext);
    var cart = carts.GetOrAdd(buyerId, id => new CartState { BuyerId = id });
    return Results.Ok(ToCartDto(cart));
});

app.MapPost("/cart/items", (HttpContext httpContext, AddCartItemRequest request) =>
{
    var errors = OrderRequestValidator.ValidateAddCartItem(request);
    if (errors.Count > 0)
    {
        return Results.ValidationProblem(errors);
    }

    var buyerId = ResolveBuyerId(httpContext);
    var cart = carts.GetOrAdd(buyerId, id => new CartState { BuyerId = id });

    var existing = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);
    if (existing is null)
    {
        cart.Items.Add(new CartItemState
        {
            ProductId = request.ProductId,
            Quantity = request.Quantity,
            UnitPrice = request.UnitPrice,
            Currency = request.Currency.ToUpperInvariant()
        });
    }
    else
    {
        existing.Quantity += request.Quantity;
    }

    return Results.Ok(ToCartDto(cart));
});

app.MapPatch("/cart/items/{itemId:guid}", (HttpContext httpContext, Guid itemId, UpdateCartItemRequest request) =>
{
    var errors = OrderRequestValidator.ValidateUpdateCartItem(request);
    if (errors.Count > 0)
    {
        return Results.ValidationProblem(errors);
    }

    var buyerId = ResolveBuyerId(httpContext);
    if (!carts.TryGetValue(buyerId, out var cart))
    {
        return Results.NotFound();
    }

    var item = cart.Items.FirstOrDefault(i => i.ItemId == itemId);
    if (item is null)
    {
        return Results.NotFound();
    }

    item.Quantity = request.Quantity;
    return Results.Ok(ToCartDto(cart));
});

app.MapDelete("/cart/items/{itemId:guid}", (HttpContext httpContext, Guid itemId) =>
{
    var buyerId = ResolveBuyerId(httpContext);
    if (!carts.TryGetValue(buyerId, out var cart))
    {
        return Results.NotFound();
    }

    var removedCount = cart.Items.RemoveAll(i => i.ItemId == itemId);
    return removedCount == 0 ? Results.NotFound() : Results.NoContent();
});

app.MapPost("/cart/checkout", (HttpContext httpContext, CheckoutRequest request) =>
{
    var errors = OrderRequestValidator.ValidateCheckout(request);
    if (errors.Count > 0)
    {
        return Results.ValidationProblem(errors);
    }

    var buyerId = ResolveBuyerId(httpContext);
    if (!carts.TryGetValue(buyerId, out var cart) || cart.Items.Count == 0)
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

    var currency = cart.Items.Select(i => i.Currency).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    if (currency.Length > 1)
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

    var order = new OrderState
    {
        BuyerId = buyerId,
        OrderStatus = OrderStatus.Placed,
        Amount = cart.Items.Sum(i => i.Quantity * i.UnitPrice),
        Currency = Enum.Parse<Currency>(currency[0], ignoreCase: true),
        Items = cart.Items.Select(ToCartItemDto).ToList()
    };

    orders[order.OrderId] = order;
    cart.Items.Clear();

    return Results.Created($"/orders/{order.OrderId}", new CheckoutResponse(
        order.OrderId,
        order.OrderStatus,
        order.Amount,
        order.Currency,
        RequiresPayment: true));
});

app.MapGet("/orders", (HttpContext httpContext) =>
{
    var buyerId = ResolveBuyerId(httpContext);
    var buyerOrders = orders.Values
        .Where(o => o.BuyerId == buyerId)
        .OrderByDescending(o => o.CreatedAt)
        .Select(o => new OrderDTO(o.OrderId, o.OrderStatus, o.Amount, o.Currency, o.CreatedAt))
        .ToList();

    return Results.Ok(buyerOrders);
});

app.MapGet("/orders/{orderId:guid}", (HttpContext httpContext, Guid orderId) =>
{
    var buyerId = ResolveBuyerId(httpContext);
    if (!orders.TryGetValue(orderId, out var order) || order.BuyerId != buyerId)
    {
        return Results.NotFound();
    }

    return Results.Ok(new OrderDetailsDto(
        order.OrderId,
        order.BuyerId,
        order.OrderStatus,
        order.Amount,
        order.Currency,
        order.CreatedAt,
        order.Items));
});

app.MapGet("/orders/{orderId:guid}/status", (HttpContext httpContext, Guid orderId) =>
{
    var buyerId = ResolveBuyerId(httpContext);
    if (!orders.TryGetValue(orderId, out var order) || order.BuyerId != buyerId)
    {
        return Results.NotFound();
    }

    return Results.Ok(new { orderId = order.OrderId, status = order.OrderStatus });
});

app.MapPost("/orders/{orderId:guid}/cancel", (HttpContext httpContext, Guid orderId) =>
{
    var buyerId = ResolveBuyerId(httpContext);
    if (!orders.TryGetValue(orderId, out var order) || order.BuyerId != buyerId)
    {
        return Results.NotFound();
    }

    if (order.OrderStatus is OrderStatus.Paid or OrderStatus.Fulfilled or OrderStatus.Completed)
    {
        return Results.UnprocessableEntity(new
        {
            error = new
            {
                code = "INVALID_ORDER_TRANSITION",
                message = "Only orders in Placed status can be cancelled."
            }
        });
    }

    order.OrderStatus = OrderStatus.Cancelled;
    return Results.Ok(new { orderId = order.OrderId, status = order.OrderStatus });
});

app.MapGet("/orders/{orderId:guid}/shipments", (HttpContext httpContext, Guid orderId) =>
{
    var buyerId = ResolveBuyerId(httpContext);
    if (!orders.TryGetValue(orderId, out var order) || order.BuyerId != buyerId)
    {
        return Results.NotFound();
    }

    return Results.Ok(order.Shipments);
});

app.Run();

static Guid ResolveBuyerId(HttpContext httpContext)
{
    var headerValue = httpContext.Request.Headers["X-User-Id"].FirstOrDefault();
    return Guid.TryParse(headerValue, out var parsed)
        ? parsed
        : Guid.Parse("11111111-1111-1111-1111-111111111111");
}

static CartDTO ToCartDto(CartState cart)
{
    var items = cart.Items.Select(ToCartItemDto).ToList();
    var currency = items.Select(i => i.Currency).FirstOrDefault();
    var total = items.Sum(i => i.LineTotal);

    return new CartDTO(cart.CartId, cart.BuyerId, items, total, currency);
}

static CartItemDTO ToCartItemDto(CartItemState item)
{
    return new CartItemDTO(
        item.ItemId,
        item.ProductId,
        item.Quantity,
        item.UnitPrice,
        item.Quantity * item.UnitPrice,
        Enum.Parse<Currency>(item.Currency, ignoreCase: true));
}
