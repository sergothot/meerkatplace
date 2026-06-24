using UserService.API.Domain.Entities;

namespace UserService.API.Application.Abstractions;

public interface IAddressRepository
{
    Task<IReadOnlyList<Address>> ListByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<Address?> GetByIdForUserAsync(Guid addressId, Guid userId, CancellationToken cancellationToken = default);

    Task AddAsync(Address address, CancellationToken cancellationToken = default);

    void Remove(Address address);
}
