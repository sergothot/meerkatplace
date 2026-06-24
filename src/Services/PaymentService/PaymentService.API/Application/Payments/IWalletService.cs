namespace PaymentService.API.Application.Payments;

public interface IWalletService
{
    Task<IResult> GetWalletAsync(HttpContext httpContext);

    Task<IResult> TopUpAsync(HttpContext httpContext, WalletTopUpRequest request);

    Task<IResult> WithdrawAsync(HttpContext httpContext, WalletWithdrawRequest request);
}
