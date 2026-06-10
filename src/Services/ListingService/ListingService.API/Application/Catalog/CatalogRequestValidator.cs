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
}
