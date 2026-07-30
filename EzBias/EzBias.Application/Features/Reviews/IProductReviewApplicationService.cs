using EzBias.Application.Common.Results;
using EzBias.Application.Features.Reviews.Dtos;

namespace EzBias.Application.Features.Reviews;

public interface IProductReviewApplicationService
{
    Task<ProductReviewSummary> GetSummaryAsync(long productId, CancellationToken ct);
    Task<ReviewEligibility> GetEligibilityAsync(long userId, long productId, CancellationToken ct);
    Task<Result<ProductReviewResponse>> CreateAsync(long userId, long productId, CreateProductReviewRequest request, CancellationToken ct);
    Task<Result<ProductReviewResponse>> UpdateAsync(long userId, long reviewId, UpdateProductReviewRequest request, CancellationToken ct);
    Task<Result> DeleteAsync(long userId, long reviewId, CancellationToken ct);
    Task<IReadOnlyList<AdminReviewListItem>> GetAllForAdminAsync(CancellationToken ct);
    Task<Result> AdminDeleteAsync(long reviewId, CancellationToken ct);
}
