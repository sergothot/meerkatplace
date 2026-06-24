namespace UserService.API.Application.Users;

public interface IUserProfileService
{
    Task<IResult> GetCurrentProfileAsync(Guid? currentUserId);

    Task<IResult> UpdateProfileAsync(Guid? currentUserId, UpdateProfileRequest request);

    Task<IResult> CreateSellerProfileAsync(Guid? currentUserId, CreateSellerProfileRequest request);

    Task<IResult> GetAddressesAsync(Guid? currentUserId);

    Task<IResult> CreateAddressAsync(Guid? currentUserId, CreateAddressRequest request);

    Task<IResult> UpdateAddressAsync(Guid? currentUserId, Guid addressId, UpdateAddressRequest request);

    Task<IResult> DeleteAddressAsync(Guid? currentUserId, Guid addressId);
}
