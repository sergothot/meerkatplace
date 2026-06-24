namespace OrderService.API.Application.Ordering;

public interface ICartService
{
    Task<IResult> GetCartAsync(HttpContext httpContext);

    Task<IResult> AddCartItemAsync(HttpContext httpContext, AddCartItemRequest request);

    Task<IResult> UpdateCartItemAsync(HttpContext httpContext, Guid itemId, UpdateCartItemRequest request);

    Task<IResult> DeleteCartItemAsync(HttpContext httpContext, Guid itemId);

    Task<IResult> CheckoutAsync(HttpContext httpContext, CheckoutRequest request);
}
