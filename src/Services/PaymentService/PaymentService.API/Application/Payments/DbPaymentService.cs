using System.Security.Claims;
using Common.Shared.Domain.Enums;
using Common.Shared.Application.Interfaces;
using PaymentService.API.Application.Abstractions;
using PaymentService.API.Domain.Entities;
using PaymentService.API.Domain.Enums;

namespace PaymentService.API.Application.Payments;

public sealed class DbPaymentService(
    IPaymentTransactionRepository payments,
    IWalletRepository wallets,
    IUnitOfWork unitOfWork) : IPaymentService
{
    public async Task<IResult> CreatePaymentAsync(HttpContext httpContext, CreatePaymentRequest request)
    {
        if (!TryResolveUserId(httpContext, out var userId))
        {
            return Results.Unauthorized();
        }

        var errors = PaymentRequestValidator.ValidateCreatePayment(request);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        if (!TryParseCurrency(request.Currency, out var currency))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["currency"] = ["Currency is not supported."]
            });
        }

        if (!TryParsePaymentMethod(request.Method, out var method))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["method"] = ["Payment method is not supported."]
            });
        }

        PaymentTransaction payment;
        try
        {
            payment = PaymentTransaction.CreatePending(request.OrderId, request.Amount, currency, method);
        }
        catch (InvalidOperationException ex)
        {
            return Results.UnprocessableEntity(new
            {
                error = new
                {
                    code = "INVALID_PAYMENT",
                    message = ex.Message
                }
            });
        }

        if (method == PaymentMethod.Wallet)
        {
            var wallet = await wallets.GetByUserIdAsync(userId);
            if (wallet is null)
            {
                wallet = Wallet.Create(userId, currency);
                await wallets.AddAsync(wallet);
            }

            if (wallet.TryDebit(request.Amount, currency))
            {
                payment.MarkSucceeded();
            }
            else
            {
                payment.MarkFailed();
            }
        }
        else
        {
            payment.MarkSucceeded();
        }

        await payments.AddAsync(payment);
        await unitOfWork.SaveChangesAsync();

        return Results.Ok(ToPaymentResponse(payment));
    }

    public async Task<IResult> GetPaymentAsync(Guid paymentId)
    {
        var payment = await payments.GetByIdAsync(paymentId);

        if (payment is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(ToPaymentResponse(payment));
    }

    public async Task<IResult> RefundPaymentAsync(HttpContext httpContext, Guid paymentId)
    {
        if (!TryResolveUserId(httpContext, out var userId))
        {
            return Results.Unauthorized();
        }

        var payment = await payments.GetByIdAsync(paymentId);

        if (payment is null)
        {
            return Results.NotFound();
        }

        if (payment.Status != PaymentStatus.Succeeded)
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

        payment.MarkRefunded();

        if (payment.PaymentMethod == PaymentMethod.Wallet)
        {
            var wallet = await wallets.GetByUserIdAsync(userId);
            if (wallet is null)
            {
                wallet = Wallet.Create(userId, payment.Currency, payment.Amount);
                await wallets.AddAsync(wallet);
            }
            else
            {
                wallet.Credit(payment.Amount, payment.Currency);
            }
        }

        await unitOfWork.SaveChangesAsync();
        return Results.Ok(ToPaymentResponse(payment));
    }

    private static PaymentResponse ToPaymentResponse(PaymentTransaction payment)
    {
        return new PaymentResponse(
            payment.Id,
            payment.OrderId,
            payment.Status.ToString(),
            payment.Amount,
            payment.Currency.ToString(),
            payment.PaymentMethod.ToString(),
            payment.CreatedAt);
    }

    private static bool TryParseCurrency(string value, out Currency currency)
    {
        return Enum.TryParse(value, true, out currency);
    }

    private static bool TryParsePaymentMethod(string value, out PaymentMethod method)
    {
        return Enum.TryParse(value, true, out method);
    }

    private static bool TryResolveUserId(HttpContext httpContext, out Guid userId)
    {
        var claim = httpContext.User.FindFirstValue("sub")
            ?? httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(claim, out userId);
    }
}
