namespace PaymentService.API.Application.Payments;

public interface IPaymentService
{
    Task<IResult> CreatePaymentAsync(HttpContext httpContext, CreatePaymentRequest request);

    Task<IResult> GetPaymentAsync(Guid paymentId);

    Task<IResult> RefundPaymentAsync(HttpContext httpContext, Guid paymentId);
}
