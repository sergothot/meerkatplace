using UserService.API.Domain.Enums;

namespace UserService.API.Application.DTOs;

public record UserProfileResponse(
    Guid Id,
    string Login,
    string Email,
    IReadOnlyList<UserRole> Roles,
    DateTimeOffset CreatedAt);

public record UpdateProfileRequest(string? Login);

public record CreateSellerProfileRequest(string StoreName);

public record SellerProfileResponse(
    Guid Id,
    Guid UserId,
    string StoreName,
    decimal Rating);

public record AddressDto(
    Guid Id,
    string Country,
    string City,
    string Street,
    string PostalCode);

public record CreateAddressRequest(
    string Country,
    string City,
    string Street,
    string PostalCode);

public record UpdateAddressRequest(
    string? Country,
    string? City,
    string? Street,
    string? PostalCode);
