namespace ListingService.API.Application.Catalog;

public static class CatalogRequestValidator
{
    public static Dictionary<string, string[]> ValidateFilter(ProductFilter filter)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        if (filter.MinPrice.HasValue && filter.MinPrice.Value < 0)
        {
            errors["minPrice"] = new[] { "minPrice cannot be negative." };
        }

        if (filter.MaxPrice.HasValue && filter.MaxPrice.Value < 0)
        {
            errors["maxPrice"] = new[] { "maxPrice cannot be negative." };
        }

        if (filter.MinPrice.HasValue && filter.MaxPrice.HasValue && filter.MinPrice.Value > filter.MaxPrice.Value)
        {
            errors["priceRange"] = new[] { "minPrice cannot be greater than maxPrice." };
        }

        if (!string.IsNullOrWhiteSpace(filter.Currency) && (filter.Currency.Length < 3 || filter.Currency.Length > 5))
        {
            errors["currency"] = new[] { "currency must be 3-5 characters." };
        }

        if (!string.IsNullOrWhiteSpace(filter.DeliveryType) &&
            !string.Equals(filter.DeliveryType, "Physical", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(filter.DeliveryType, "Digital", StringComparison.OrdinalIgnoreCase))
        {
            errors["deliveryType"] = new[] { "deliveryType must be Physical or Digital." };
        }

        return errors;
    }

    public static Dictionary<string, string[]> ValidateCreateProduct(CreateProductRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors["name"] = ["Name is required."];
        }

        if (request.Price <= 0)
        {
            errors["price"] = ["Price must be greater than zero."];
        }

        if (string.IsNullOrWhiteSpace(request.Currency) || request.Currency.Length is < 3 or > 5)
        {
            errors["currency"] = ["Currency must be 3-5 characters."];
        }

        if (!string.Equals(request.DeliveryType, "Physical", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(request.DeliveryType, "Digital", StringComparison.OrdinalIgnoreCase))
        {
            errors["deliveryType"] = ["DeliveryType must be Physical or Digital."];
        }

        if (request.StockQuantity.HasValue && request.StockQuantity.Value < 0)
        {
            errors["stockQuantity"] = ["Stock quantity cannot be negative."];
        }

        return errors;
    }

    public static Dictionary<string, string[]> ValidateUpdateProduct(UpdateProductRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        if (request.Price.HasValue && request.Price.Value <= 0)
        {
            errors["price"] = ["Price must be greater than zero."];
        }

        if (!string.IsNullOrWhiteSpace(request.Currency) && (request.Currency.Length < 3 || request.Currency.Length > 5))
        {
            errors["currency"] = ["Currency must be 3-5 characters."];
        }

        if (!string.IsNullOrWhiteSpace(request.DeliveryType) &&
            !string.Equals(request.DeliveryType, "Physical", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(request.DeliveryType, "Digital", StringComparison.OrdinalIgnoreCase))
        {
            errors["deliveryType"] = ["DeliveryType must be Physical or Digital."];
        }

        if (!string.IsNullOrWhiteSpace(request.Status) &&
            !string.Equals(request.Status, "Active", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(request.Status, "Inactive", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(request.Status, "Draft", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(request.Status, "Archived", StringComparison.OrdinalIgnoreCase))
        {
            errors["status"] = ["Status must be Active, Inactive, Draft, or Archived."];
        }

        return errors;
    }

    public static Dictionary<string, string[]> ValidateUpdateStock(UpdateStockRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        if (request.Quantity < 0)
        {
            errors["quantity"] = ["Quantity cannot be negative."];
        }

        if (request.Reserved < 0)
        {
            errors["reserved"] = ["Reserved cannot be negative."];
        }

        if (request.Reserved > request.Quantity)
        {
            errors["reserved"] = ["Reserved cannot be greater than quantity."];
        }

        return errors;
    }
}
