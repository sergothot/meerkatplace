namespace OrderService.API.Application.Ordering;

public static class OrderRequestValidator
{
    public static Dictionary<string, string[]> ValidateAddCartItem(AddCartItemRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        if (request.ProductId == Guid.Empty)
        {
            errors["productId"] = new[] { "ProductId is required." };
        }

        if (request.Quantity <= 0)
        {
            errors["quantity"] = new[] { "Quantity must be greater than zero." };
        }

        if (request.UnitPrice <= 0)
        {
            errors["unitPrice"] = new[] { "UnitPrice must be greater than zero." };
        }

        if (string.IsNullOrWhiteSpace(request.Currency) || request.Currency.Length is < 3 or > 5)
        {
            errors["currency"] = new[] { "Currency must be a 3-5 character ISO-like code." };
        }

        return errors;
    }

    public static Dictionary<string, string[]> ValidateUpdateCartItem(UpdateCartItemRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        if (request.Quantity <= 0)
        {
            errors["quantity"] = new[] { "Quantity must be greater than zero." };
        }

        return errors;
    }

    public static Dictionary<string, string[]> ValidateCheckout(CheckoutRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(request.AddressId))
        {
            errors["addressId"] = new[] { "AddressId is required." };
        }

        if (string.IsNullOrWhiteSpace(request.PaymentMethod))
        {
            errors["paymentMethod"] = new[] { "PaymentMethod is required." };
        }

        return errors;
    }
}