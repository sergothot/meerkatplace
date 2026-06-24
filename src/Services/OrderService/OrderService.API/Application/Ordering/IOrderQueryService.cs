namespace OrderService.API.Application.Ordering;

public interface IOrderQueryService
{
    Task<IResult> GetOrdersAsync(HttpContext httpContext);

    Task<IResult> GetOrderAsync(HttpContext httpContext, Guid orderId);

    Task<IResult> GetOrderStatusAsync(HttpContext httpContext, Guid orderId);

    Task<IResult> CancelOrderAsync(HttpContext httpContext, Guid orderId);

    Task<IResult> GetShipmentsAsync(HttpContext httpContext, Guid orderId);
}
