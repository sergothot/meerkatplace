namespace PaymentService.API.Application.Payments;

public static class PaymentRequestValidator
{
    public static Dictionary<string, string[]> ValidateCreatePayment(CreatePaymentRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        if (request.OrderId == Guid.Empty)
        {
            errors["orderId"] = new[] { "OrderId is required." };
        }

        if (string.IsNullOrWhiteSpace(request.Method))
        {
            errors["method"] = new[] { "Method is required." };
        }

        if (request.Amount <= 0)
        {
            errors["amount"] = new[] { "Amount must be greater than zero." };
        }

        if (string.IsNullOrWhiteSpace(request.Currency) || request.Currency.Length is < 3 or > 5)
        {
            errors["currency"] = new[] { "Currency must be a 3-5 character ISO-like code." };
        }

        return errors;
    }

    public static Dictionary<string, string[]> ValidateWalletAmount(decimal amount, string currency)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        if (amount <= 0)
        {
            errors["amount"] = new[] { "Amount must be greater than zero." };
        }

        if (string.IsNullOrWhiteSpace(currency) || currency.Length is < 3 or > 5)
        {
            errors["currency"] = new[] { "Currency must be a 3-5 character ISO-like code." };
        }

        return errors;
    }
}
