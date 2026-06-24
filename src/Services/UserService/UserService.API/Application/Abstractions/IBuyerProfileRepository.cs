using UserService.API.Domain.Entities;

namespace UserService.API.Application.Abstractions;

public interface IBuyerProfileRepository
{
    Task AddAsync(BuyerProfile profile, CancellationToken cancellationToken = default);
}
