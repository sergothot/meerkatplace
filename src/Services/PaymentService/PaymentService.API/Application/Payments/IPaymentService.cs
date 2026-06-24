namespace PaymentService.API.Application.Payments;

public interface IPaymentService
{
    Task<IResult> CreatePaymentAsync(CreatePaymentRequest request);

    Task<IResult> GetPaymentAsync(Guid paymentId);

    Task<IResult> RefundPaymentAsync(Guid paymentId);
}
