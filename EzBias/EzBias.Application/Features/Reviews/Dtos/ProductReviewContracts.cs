namespace EzBias.Application.Features.Reviews.Dtos;

public record CreateProductReviewRequest(short Stars, string? Comment);

public record UpdateProductReviewRequest(short Stars, string? Comment);

public record ProductReviewResponse(
    long Id,
    long ProductId,
    long UserId,
    string Username,
    short Stars,
    string? Comment,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public record ProductReviewSummary(
    long ProductId,
    double AverageStars,
    int TotalReviews,
    IReadOnlyList<ProductReviewResponse> Reviews);

public record ReviewEligibility(bool HasPurchased, ProductReviewResponse? ExistingReview);

public record AdminReviewListItem(
    long Id,
    long ProductId,
    string ProductName,
    long UserId,
    string Username,
    short Stars,
    string? Comment,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt
);
