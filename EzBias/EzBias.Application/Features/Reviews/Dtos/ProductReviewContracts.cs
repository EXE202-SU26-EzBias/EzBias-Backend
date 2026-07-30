using EzBias.Application.Features.Media;
using EzBias.Domain.Enums;

namespace EzBias.Application.Features.Reviews.Dtos;

public record StoredReviewMedia(
    ReviewMediaType MediaType,
    string Url,
    string? ThumbnailUrl,
    string CloudinaryPublicId);

public record CreateProductReviewRequest(short Stars, string? Comment, IReadOnlyList<UploadFile> Media);

public record UpdateProductReviewRequest(
    short Stars,
    string? Comment,
    IReadOnlyList<long> KeepMediaIds,
    IReadOnlyList<UploadFile> NewMedia);

public record ProductReviewMediaResponse(
    long Id,
    string Type,
    string Url,
    string? ThumbnailUrl,
    short SortOrder);

public record ProductReviewResponse(
    long Id,
    long ProductId,
    long UserId,
    string Username,
    short Stars,
    string? Comment,
    IReadOnlyList<ProductReviewMediaResponse> Media,
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
    IReadOnlyList<ProductReviewMediaResponse> Media,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt
);
