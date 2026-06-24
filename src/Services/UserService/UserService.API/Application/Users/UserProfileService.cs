using Common.Shared.Application.Interfaces;
using UserService.API.Application.Abstractions;
using UserService.API.Domain.Entities;
using UserService.API.Domain.Enums;

namespace UserService.API.Application.Users;

public sealed class UserProfileService(
    IUserRepository users,
    ISellerProfileRepository sellers,
    IAddressRepository addresses,
    IUnitOfWork unitOfWork) : IUserProfileService
{
    public async Task<IResult> GetCurrentProfileAsync(Guid? currentUserId)
    {
        if (currentUserId is null)
            return Results.Unauthorized();

        var user = await users.GetByIdAsync(currentUserId.Value);
        return user is null ? Results.NotFound() : Results.Ok(ToProfileResponse(user));
    }

    public async Task<IResult> UpdateProfileAsync(Guid? currentUserId, UpdateProfileRequest request)
    {
        if (currentUserId is null)
            return Results.Unauthorized();

        var errors = UserRequestValidator.ValidateUpdateProfile(request);
        if (errors.Count > 0)
            return Results.ValidationProblem(errors);

        var user = await users.GetByIdAsync(currentUserId.Value);
        if (user is null)
            return Results.NotFound();

        if (request.Login is not null)
        {
            if (await users.AnyByLoginAsync(request.Login, currentUserId))
                return Results.Conflict(new { error = new { code = "DUPLICATE_LOGIN", message = "Login is already taken." } });

            user.Login = request.Login;
        }

        await unitOfWork.SaveChangesAsync();
        return Results.Ok(ToProfileResponse(user));
    }

    public async Task<IResult> CreateSellerProfileAsync(Guid? currentUserId, CreateSellerProfileRequest request)
    {
        if (currentUserId is null)
            return Results.Unauthorized();

        var errors = UserRequestValidator.ValidateCreateSellerProfile(request);
        if (errors.Count > 0)
            return Results.ValidationProblem(errors);

        var user = await users.GetByIdAsync(currentUserId.Value);
        if (user is null)
            return Results.NotFound();

        if (user.Roles.Contains(UserRole.Seller))
            return Results.Conflict(new { error = new { code = "ALREADY_SELLER", message = "User is already a seller." } });

        if (await sellers.AnyByStoreNameAsync(request.StoreName))
            return Results.Conflict(new { error = new { code = "DUPLICATE_STORE_NAME", message = "Store name is already taken." } });

        var profile = new SellerProfile { UserId = user.Id, StoreName = request.StoreName };
        await sellers.AddAsync(profile);

        user.Roles = [.. user.Roles, UserRole.Seller];
        await unitOfWork.SaveChangesAsync();

        return Results.Ok(new SellerProfileResponse(profile.Id, profile.UserId, profile.StoreName, profile.Rating));
    }

    public async Task<IResult> GetAddressesAsync(Guid? currentUserId)
    {
        if (currentUserId is null)
            return Results.Unauthorized();

        var userAddresses = await addresses.ListByUserIdAsync(currentUserId.Value);

        return Results.Ok(userAddresses.Select(ToAddressDto));
    }

    public async Task<IResult> CreateAddressAsync(Guid? currentUserId, CreateAddressRequest request)
    {
        if (currentUserId is null)
            return Results.Unauthorized();

        var errors = UserRequestValidator.ValidateCreateAddress(request);
        if (errors.Count > 0)
            return Results.ValidationProblem(errors);

        Address address;
        try
        {
            address = Address.Create(
                currentUserId.Value,
                request.Country,
                request.City,
                request.Street,
                request.PostalCode);
        }
        catch (InvalidOperationException ex)
        {
            return Results.UnprocessableEntity(new
            {
                error = new
                {
                    code = "INVALID_ADDRESS",
                    message = ex.Message
                }
            });
        }

        await addresses.AddAsync(address);
        await unitOfWork.SaveChangesAsync();

        return Results.Created($"/users/me/addresses/{address.Id}", ToAddressDto(address));
    }

    public async Task<IResult> UpdateAddressAsync(Guid? currentUserId, Guid addressId, UpdateAddressRequest request)
    {
        if (currentUserId is null)
            return Results.Unauthorized();

        var address = await addresses.GetByIdForUserAsync(addressId, currentUserId.Value);
        if (address is null)
            return Results.NotFound();

        try
        {
            address.Update(
                request.Country ?? address.Country,
                request.City ?? address.City,
                request.Street ?? address.Street,
                request.PostalCode ?? address.PostalCode);
        }
        catch (InvalidOperationException ex)
        {
            return Results.UnprocessableEntity(new
            {
                error = new
                {
                    code = "INVALID_ADDRESS_UPDATE",
                    message = ex.Message
                }
            });
        }

        await unitOfWork.SaveChangesAsync();
        return Results.Ok(ToAddressDto(address));
    }

    public async Task<IResult> DeleteAddressAsync(Guid? currentUserId, Guid addressId)
    {
        if (currentUserId is null)
            return Results.Unauthorized();

        var address = await addresses.GetByIdForUserAsync(addressId, currentUserId.Value);
        if (address is null)
            return Results.NotFound();

        addresses.Remove(address);
        await unitOfWork.SaveChangesAsync();
        return Results.NoContent();
    }

    private static UserProfileResponse ToProfileResponse(User user) =>
        new(user.Id, user.Login, user.Email, user.Roles, user.CreatedAt);

    private static AddressDto ToAddressDto(Address address) =>
        new(address.Id, address.Country, address.City, address.Street, address.PostalCode);
}
