using OrderService.API.Application.Ordering;

namespace OrderService.API.Presentation.Endpoints;

public static class OrderEndpoints
{
    public static void MapOrderEndpoints(this WebApplication app)
    {
        app.MapGet("/", () => "Hello World!");
        app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "order-service" }));

        var cartGroup = app.MapGroup("/cart").RequireAuthorization("buyer");
        cartGroup.MapGet("", (HttpContext httpContext, ICartService cartsService) =>
            cartsService.GetCartAsync(httpContext));

        cartGroup.MapPost("/items", (HttpContext httpContext, AddCartItemRequest request, ICartService cartsService) =>
            cartsService.AddCartItemAsync(httpContext, request))
            .WithSummary("Add cart item")
            .WithDescription("Adds or merges a product into current buyer cart.");

        cartGroup.MapPatch("/items/{itemId:guid}", (HttpContext httpContext, Guid itemId, UpdateCartItemRequest request, ICartService cartsService) =>
            cartsService.UpdateCartItemAsync(httpContext, itemId, request));

        cartGroup.MapDelete("/items/{itemId:guid}", (HttpContext httpContext, Guid itemId, ICartService cartsService) =>
            cartsService.DeleteCartItemAsync(httpContext, itemId));

        cartGroup.MapPost("/checkout", (HttpContext httpContext, CheckoutRequest request, ICartService cartsService) =>
            cartsService.CheckoutAsync(httpContext, request))
            .WithSummary("Checkout cart")
            .WithDescription("Places an order and starts asynchronous checkout orchestration.");

        var ordersGroup = app.MapGroup("/orders").RequireAuthorization("buyer");
        ordersGroup.MapGet("", (HttpContext httpContext, IOrderQueryService ordersService) =>
            ordersService.GetOrdersAsync(httpContext))
            .WithSummary("List orders")
            .WithDescription("Returns orders for current authenticated buyer.");

        ordersGroup.MapGet("/{orderId:guid}", (HttpContext httpContext, Guid orderId, IOrderQueryService ordersService) =>
            ordersService.GetOrderAsync(httpContext, orderId));

        ordersGroup.MapGet("/{orderId:guid}/status", (HttpContext httpContext, Guid orderId, IOrderQueryService ordersService) =>
            ordersService.GetOrderStatusAsync(httpContext, orderId))
            .WithSummary("Get order status")
            .WithDescription("Returns current order status for polling checkout progression.");

        ordersGroup.MapPost("/{orderId:guid}/cancel", (HttpContext httpContext, Guid orderId, IOrderQueryService ordersService) =>
            ordersService.CancelOrderAsync(httpContext, orderId));

        ordersGroup.MapGet("/{orderId:guid}/shipments", (HttpContext httpContext, Guid orderId, IOrderQueryService ordersService) =>
            ordersService.GetShipmentsAsync(httpContext, orderId))
            .WithSummary("List shipments")
            .WithDescription("Returns shipment records related to the order.");
    }
}
