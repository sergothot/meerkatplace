using ListingService.API.Domain.Entities;

namespace ListingService.API.Application.Abstractions.Repositories;

public interface IReviewRepository
{
    Task<IReadOnlyCollection<Review>> GetByProductAsync(
        Guid productId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Review>> GetBySellerAsync(
        Guid sellerId,
        CancellationToken cancellationToken = default);

    Task CreateAsync(
        Review review,
        CancellationToken cancellationToken = default);
}