using EzBias.Application.Features.Reviews.Dtos;

namespace EzBias.Application.Features.Reviews;

public interface IProductReviewApplicationService
{
    Task<ProductReviewSummary> GetSummaryAsync(long productId, CancellationToken ct);
    Task<ReviewEligibility> GetEligibilityAsync(long userId, long productId, CancellationToken ct);
    Task<(bool Success, string? Error, ProductReviewResponse? Data)> CreateAsync(long userId, long productId, CreateProductReviewRequest request, CancellationToken ct);
    Task<(bool Success, string? Error, ProductReviewResponse? Data)> UpdateAsync(long userId, long reviewId, UpdateProductReviewRequest request, CancellationToken ct);
    Task<(bool Success, string? Error)> DeleteAsync(long userId, long reviewId, CancellationToken ct);
    Task<IReadOnlyList<AdminReviewListItem>> GetAllForAdminAsync(CancellationToken ct);
    Task<(bool Success, string? Error)> AdminDeleteAsync(long reviewId, CancellationToken ct);
}
