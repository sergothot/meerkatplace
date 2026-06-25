using PaymentService.API.Application.Payments;

namespace PaymentService.API.Presentation.Endpoints;

public static class PaymentEndpoints
{
    public static void MapPaymentEndpoints(this WebApplication app)
    {
        app.MapGet("/", () => "Hello World!");
        app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "payment-service" }));

        var paymentsGroup = app.MapGroup("/payments").RequireAuthorization();
        paymentsGroup.MapPost("", (HttpContext httpContext, CreatePaymentRequest request, IPaymentService paymentsService) =>
            paymentsService.CreatePaymentAsync(httpContext, request))
            .WithSummary("Create payment")
            .WithDescription("Creates and processes payment for an order.");

        paymentsGroup.MapGet("/{paymentId:guid}", (Guid paymentId, IPaymentService paymentsService) =>
            paymentsService.GetPaymentAsync(paymentId))
            .WithSummary("Get payment")
            .WithDescription("Returns payment transaction details by id.");

        paymentsGroup.MapPost("/{paymentId:guid}/refund", (HttpContext httpContext, Guid paymentId, IPaymentService paymentsService) =>
            paymentsService.RefundPaymentAsync(httpContext, paymentId));

        var walletGroup = app.MapGroup("/wallet").RequireAuthorization();
        walletGroup.MapGet("", (HttpContext httpContext, IWalletService walletsService) =>
            walletsService.GetWalletAsync(httpContext))
            .WithSummary("Get wallet")
            .WithDescription("Returns current user wallet balance.");

        walletGroup.MapPost("/topup", (HttpContext httpContext, WalletTopUpRequest request, IWalletService walletsService) =>
            walletsService.TopUpAsync(httpContext, request));

        walletGroup.MapPost("/withdraw", (HttpContext httpContext, WalletWithdrawRequest request, IWalletService walletsService) =>
            walletsService.WithdrawAsync(httpContext, request));
    }
}
