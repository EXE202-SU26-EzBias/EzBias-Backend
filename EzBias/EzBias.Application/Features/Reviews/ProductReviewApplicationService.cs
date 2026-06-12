using EzBias.Application.Features.Reviews.Dtos;
using EzBias.Domain.Entities;
using EzBias.Domain.Interfaces;

namespace EzBias.Application.Features.Reviews;

public class ProductReviewApplicationService : IProductReviewApplicationService
{
    private readonly IProductReviewRepository _reviews;
    private readonly IOrderRepository _orders;
    private readonly IProductRepository _products;
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _uow;

    public ProductReviewApplicationService(
        IProductReviewRepository reviews,
        IOrderRepository orders,
        IProductRepository products,
        IUserRepository users,
        IUnitOfWork uow)
    {
        _reviews = reviews;
        _orders = orders;
        _products = products;
        _users = users;
        _uow = uow;
    }

    public async Task<ProductReviewSummary> GetSummaryAsync(long productId, CancellationToken ct)
    {
        var reviews = await _reviews.GetByProductIdAsync(productId, ct);
        var average = reviews.Count > 0 ? reviews.Average(x => x.Stars) : 0d;
        var mapped = reviews.Select(x => Map(x, x.User.Username)).ToList();
        return new ProductReviewSummary(productId, Math.Round(average, 2), reviews.Count, mapped);
    }

    public async Task<ReviewEligibility> GetEligibilityAsync(long userId, long productId, CancellationToken ct)
    {
        var hasPurchased = await _orders.HasUserPurchasedProductAsync(userId, productId, ct);
        var existing = await _reviews.GetByProductAndUserAsync(productId, userId, ct);
        var mapped = existing is null ? null : Map(existing, existing.User.Username);
        return new ReviewEligibility(hasPurchased, mapped);
    }

    public async Task<(bool Success, string? Error, ProductReviewResponse? Data)> CreateAsync(long userId, long productId, CreateProductReviewRequest request, CancellationToken ct)
    {
        if (request.Stars < 1 || request.Stars > 5)
            return (false, "Stars must be between 1 and 5.", null);

        var product = await _products.GetByIdAsync(productId, ct);
        if (product is null) return (false, "Product not found.", null);

        var hasPurchased = await _orders.HasUserPurchasedProductAsync(userId, productId, ct);
        if (!hasPurchased) return (false, "Forbidden.", null);

        var existing = await _reviews.GetByProductAndUserAsync(productId, userId, ct);
        if (existing is not null) return (false, "Already reviewed.", null);

        var review = new ProductReview
        {
            ProductId = productId,
            UserId = userId,
            Stars = request.Stars,
            Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        _reviews.Add(review);
        await _uow.SaveChangesAsync(ct);

        var user = await _users.GetByIdAsync(userId, ct);
        return (true, null, Map(review, user?.Username ?? string.Empty));
    }

    public async Task<(bool Success, string? Error, ProductReviewResponse? Data)> UpdateAsync(long userId, long reviewId, UpdateProductReviewRequest request, CancellationToken ct)
    {
        if (request.Stars < 1 || request.Stars > 5)
            return (false, "Stars must be between 1 and 5.", null);

        var review = await _reviews.GetByIdAsync(reviewId, ct);
        if (review is null) return (false, "Review not found.", null);
        if (review.UserId != userId) return (false, "Forbidden.", null);

        review.Stars = request.Stars;
        review.Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim();
        review.UpdatedAt = DateTimeOffset.UtcNow;

        await _uow.SaveChangesAsync(ct);

        var user = await _users.GetByIdAsync(userId, ct);
        return (true, null, Map(review, user?.Username ?? string.Empty));
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(long userId, long reviewId, CancellationToken ct)
    {
        var review = await _reviews.GetByIdAsync(reviewId, ct);
        if (review is null) return (false, "Review not found.");
        if (review.UserId != userId) return (false, "Forbidden.");

        _reviews.Remove(review);
        await _uow.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task<IReadOnlyList<AdminReviewListItem>> GetAllForAdminAsync(CancellationToken ct)
    {
        var all = await _reviews.GetAllAsync(ct);
        return all.Select(r => new AdminReviewListItem(
            r.Id,
            r.ProductId,
            r.Product?.Name ?? string.Empty,
            r.UserId,
            r.User?.Username ?? string.Empty,
            r.Stars,
            r.Comment,
            r.CreatedAt,
            r.UpdatedAt
        )).ToList();
    }

    public async Task<(bool Success, string? Error)> AdminDeleteAsync(long reviewId, CancellationToken ct)
    {
        var review = await _reviews.GetByIdAsync(reviewId, ct);
        if (review is null) return (false, "Review not found.");

        _reviews.Remove(review);
        await _uow.SaveChangesAsync(ct);
        return (true, null);
    }

    private static ProductReviewResponse Map(ProductReview x, string username)
        => new(x.Id, x.ProductId, x.UserId, username, x.Stars, x.Comment, x.CreatedAt, x.UpdatedAt);
}
