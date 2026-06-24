using UserService.API.Domain.Entities;

namespace UserService.API.Application.Abstractions;

public interface ISellerProfileRepository
{
    Task<bool> AnyByStoreNameAsync(string storeName, CancellationToken cancellationToken = default);

    Task AddAsync(SellerProfile profile, CancellationToken cancellationToken = default);
}
