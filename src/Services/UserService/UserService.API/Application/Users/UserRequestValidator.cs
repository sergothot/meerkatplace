namespace UserService.API.Application.Users;

public static class UserRequestValidator
{
    public static Dictionary<string, string[]> ValidateUpdateProfile(UpdateProfileRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        if (request.Login is not null && (request.Login.Length < 3 || request.Login.Length > 50))
            errors["login"] = ["Login must be between 3 and 50 characters."];

        return errors;
    }

    public static Dictionary<string, string[]> ValidateCreateSellerProfile(CreateSellerProfileRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(request.StoreName) || request.StoreName.Length < 2 || request.StoreName.Length > 100)
            errors["storeName"] = ["Store name must be between 2 and 100 characters."];

        return errors;
    }

    public static Dictionary<string, string[]> ValidateCreateAddress(CreateAddressRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(request.Country))
            errors["country"] = ["Country is required."];

        if (string.IsNullOrWhiteSpace(request.City))
            errors["city"] = ["City is required."];

        if (string.IsNullOrWhiteSpace(request.Street))
            errors["street"] = ["Street is required."];

        if (string.IsNullOrWhiteSpace(request.PostalCode))
            errors["postalCode"] = ["Postal code is required."];

        return errors;
    }
}
