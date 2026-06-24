using Common.Shared.Domain.Enums;
using Common.Shared.Application.Interfaces;
using PaymentService.API.Application.Abstractions;
using PaymentService.API.Domain.Entities;

namespace PaymentService.API.Application.Payments;

public sealed class DbWalletService(
    IWalletRepository wallets,
    IUnitOfWork unitOfWork) : IWalletService
{
    public async Task<IResult> GetWalletAsync(HttpContext httpContext)
    {
        var userId = ResolveUserId(httpContext);
        var wallet = await wallets.GetByUserIdAsync(userId);

        if (wallet is null)
        {
            wallet = Wallet.Create(userId, Currency.RUB);
            await wallets.AddAsync(wallet);
            await unitOfWork.SaveChangesAsync();
        }

        return Results.Ok(ToWalletDto(wallet));
    }

    public async Task<IResult> TopUpAsync(HttpContext httpContext, WalletTopUpRequest request)
    {
        var errors = PaymentRequestValidator.ValidateWalletAmount(request.Amount, request.Currency);
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

        var userId = ResolveUserId(httpContext);
        var wallet = await wallets.GetByUserIdAsync(userId);

        if (wallet is null)
        {
            wallet = Wallet.Create(userId, currency);
            await wallets.AddAsync(wallet);
        }

        if (wallet.Currency != currency)
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

        wallet.Credit(request.Amount, currency);
        await unitOfWork.SaveChangesAsync();
        return Results.Ok(ToWalletDto(wallet));
    }

    public async Task<IResult> WithdrawAsync(HttpContext httpContext, WalletWithdrawRequest request)
    {
        var errors = PaymentRequestValidator.ValidateWalletAmount(request.Amount, request.Currency);
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

        var userId = ResolveUserId(httpContext);
        var wallet = await wallets.GetByUserIdAsync(userId);

        if (wallet is null)
        {
            wallet = Wallet.Create(userId, currency);
            await wallets.AddAsync(wallet);
            await unitOfWork.SaveChangesAsync();
        }

        if (wallet.Currency != currency)
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

        try
        {
            wallet.Withdraw(request.Amount, currency);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Insufficient", StringComparison.OrdinalIgnoreCase))
        {
            return Results.UnprocessableEntity(new
            {
                error = new
                {
                    code = "INSUFFICIENT_FUNDS",
                    message = ex.Message
                }
            });
        }
        await unitOfWork.SaveChangesAsync();
        return Results.Ok(ToWalletDto(wallet));
    }

    private static WalletDto ToWalletDto(Wallet wallet)
    {
        return new WalletDto(wallet.UserId, wallet.Balance, wallet.Currency.ToString());
    }

    private static bool TryParseCurrency(string value, out Currency currency)
    {
        return Enum.TryParse(value, true, out currency);
    }

    private static Guid ResolveUserId(HttpContext? httpContext)
    {
        var headerValue = httpContext?.Request.Headers["X-User-Id"].FirstOrDefault();
        return Guid.TryParse(headerValue, out var parsed)
            ? parsed
            : Guid.Parse("11111111-1111-1111-1111-111111111111");
    }
}
