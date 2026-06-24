namespace UserService.API.Domain.Entities;

public class Address
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public string Country { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
    public string Street { get; private set; } = string.Empty;
    public string PostalCode { get; private set; } = string.Empty;

    public static Address Create(Guid userId, string country, string city, string street, string postalCode)
    {
        return new Address
        {
            UserId = userId,
            Country = NormalizeRequired(country, nameof(country)),
            City = NormalizeRequired(city, nameof(city)),
            Street = NormalizeRequired(street, nameof(street)),
            PostalCode = NormalizeRequired(postalCode, nameof(postalCode))
        };
    }

    public void Update(string country, string city, string street, string postalCode)
    {
        Country = NormalizeRequired(country, nameof(country));
        City = NormalizeRequired(city, nameof(city));
        Street = NormalizeRequired(street, nameof(street));
        PostalCode = NormalizeRequired(postalCode, nameof(postalCode));
    }

    private static string NormalizeRequired(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{fieldName} is required.");
        }

        return value.Trim();
    }
}
