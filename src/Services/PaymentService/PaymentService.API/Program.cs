using System.Collections.Concurrent;
using PaymentService.API.Application.Payments;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
var app = builder.Build();

var payments = new ConcurrentDictionary<Guid, PaymentState>();
var wallets = new ConcurrentDictionary<Guid, WalletDto>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/", () => "Hello World!");
app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "payment-service" }));
app.MapPost("/payments", (CreatePaymentRequest request) =>
{
    var errors = PaymentRequestValidator.ValidateCreatePayment(request);
    if (errors.Count > 0)
    {
        return Results.ValidationProblem(errors);
    }

    var normalizedCurrency = request.Currency.ToUpperInvariant();
    var normalizedMethod = request.Method.Trim();

    var payment = new PaymentState
    {
        OrderId = request.OrderId,
        Amount = request.Amount,
        Currency = normalizedCurrency,
        Method = normalizedMethod,
        Status = "Pending"
    };

    if (string.Equals(normalizedMethod, "Wallet", StringComparison.OrdinalIgnoreCase))
    {
        var userId = ResolveUserId(null);
        var wallet = wallets.GetOrAdd(userId, id => new WalletDto(id, 10000m, normalizedCurrency));

        if (!string.Equals(wallet.Currency, normalizedCurrency, StringComparison.OrdinalIgnoreCase))
        {
            payment.Status = "Failed";
        }
        else if (wallet.Balance < request.Amount)
        {
            payment.Status = "Failed";
        }
        else
        {
            wallets[userId] = wallet with { Balance = wallet.Balance - request.Amount };
            payment.Status = "Succeeded";
        }
    }
    else
    {
        payment.Status = "Pending";
    }

    payments[payment.PaymentId] = payment;

    return Results.Ok(new PaymentResponse(
        payment.PaymentId,
        payment.OrderId,
        payment.Status,
        payment.Amount,
        payment.Currency,
        payment.Method,
        payment.CreatedAt));
});

app.MapGet("/payments/{paymentId:guid}", (Guid paymentId) =>
{
    if (!payments.TryGetValue(paymentId, out var payment))
    {
        return Results.NotFound();
    }

    return Results.Ok(new PaymentResponse(
        payment.PaymentId,
        payment.OrderId,
        payment.Status,
        payment.Amount,
        payment.Currency,
        payment.Method,
        payment.CreatedAt));
});

app.MapPost("/payments/{paymentId:guid}/refund", (Guid paymentId) =>
{
    if (!payments.TryGetValue(paymentId, out var payment))
    {
        return Results.NotFound();
    }

    if (!string.Equals(payment.Status, "Succeeded", StringComparison.OrdinalIgnoreCase))
    {
        return Results.UnprocessableEntity(new
        {
            error = new
            {
                code = "REFUND_NOT_ALLOWED",
                message = "Only succeeded payments can be refunded."
            }
        });
    }

    payment.Status = "Refunded";

    if (string.Equals(payment.Method, "Wallet", StringComparison.OrdinalIgnoreCase))
    {
        var userId = ResolveUserId(null);
        var wallet = wallets.GetOrAdd(userId, id => new WalletDto(id, 0m, payment.Currency));
        wallets[userId] = wallet with { Balance = wallet.Balance + payment.Amount };
    }

    return Results.Ok(new PaymentResponse(
        payment.PaymentId,
        payment.OrderId,
        payment.Status,
        payment.Amount,
        payment.Currency,
        payment.Method,
        payment.CreatedAt));
});

app.MapGet("/wallet", (HttpContext httpContext) =>
{
    var userId = ResolveUserId(httpContext);
    var wallet = wallets.GetOrAdd(userId, id => new WalletDto(id, 10000m, "RUB"));
    return Results.Ok(wallet);
});

app.MapPost("/wallet/topup", (HttpContext httpContext, WalletTopUpRequest request) =>
{
    var errors = PaymentRequestValidator.ValidateWalletAmount(request.Amount, request.Currency);
    if (errors.Count > 0)
    {
        return Results.ValidationProblem(errors);
    }

    var userId = ResolveUserId(httpContext);
    var wallet = wallets.GetOrAdd(userId, id => new WalletDto(id, 10000m, request.Currency.ToUpperInvariant()));

    if (!string.Equals(wallet.Currency, request.Currency, StringComparison.OrdinalIgnoreCase))
    {
        return Results.UnprocessableEntity(new
        {
            error = new
            {
                code = "CURRENCY_MISMATCH",
                message = "Wallet currency mismatch."
            }
        });
    }

    var updated = wallet with { Balance = wallet.Balance + request.Amount };
    wallets[userId] = updated;
    return Results.Ok(updated);
});

app.MapPost("/wallet/withdraw", (HttpContext httpContext, WalletWithdrawRequest request) =>
{
    var errors = PaymentRequestValidator.ValidateWalletAmount(request.Amount, request.Currency);
    if (errors.Count > 0)
    {
        return Results.ValidationProblem(errors);
    }

    var userId = ResolveUserId(httpContext);
    var wallet = wallets.GetOrAdd(userId, id => new WalletDto(id, 10000m, request.Currency.ToUpperInvariant()));

    if (!string.Equals(wallet.Currency, request.Currency, StringComparison.OrdinalIgnoreCase))
    {
        return Results.UnprocessableEntity(new
        {
            error = new
            {
                code = "CURRENCY_MISMATCH",
                message = "Wallet currency mismatch."
            }
        });
    }

    if (wallet.Balance < request.Amount)
    {
        return Results.UnprocessableEntity(new
        {
            error = new
            {
                code = "INSUFFICIENT_FUNDS",
                message = "Insufficient wallet balance."
            }
        });
    }

    var updated = wallet with { Balance = wallet.Balance - request.Amount };
    wallets[userId] = updated;
    return Results.Ok(updated);
});

app.Run();

static Guid ResolveUserId(HttpContext? httpContext)
{
    var headerValue = httpContext?.Request.Headers["X-User-Id"].FirstOrDefault();
    return Guid.TryParse(headerValue, out var parsed)
        ? parsed
        : Guid.Parse("11111111-1111-1111-1111-111111111111");
}
