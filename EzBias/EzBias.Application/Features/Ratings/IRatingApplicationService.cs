using EzBias.Application.Features.Ratings.Dtos;

namespace EzBias.Application.Features.Ratings;

public interface IRatingApplicationService
{
    Task<(bool Success, string? Error, RatingResponse? Data)> CreateAsync(long buyerId, CreateRatingRequest request, CancellationToken ct);
    Task<IReadOnlyList<RatingResponse>> GetBySellerAsync(long sellerId, CancellationToken ct);
}
