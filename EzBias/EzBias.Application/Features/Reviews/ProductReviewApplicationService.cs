using EzBias.Application.Common.Results;
using EzBias.Application.Features.Media;
using EzBias.Application.Features.Reviews.Dtos;
using EzBias.Domain.Entities;
using EzBias.Domain.Enums;
using EzBias.Domain.Interfaces;

namespace EzBias.Application.Features.Reviews;

public class ProductReviewApplicationService : IProductReviewApplicationService
{
    private const int MaxMediaCount = 6;
    private const int MaxImageCount = 5;
    private const int MaxVideoCount = 1;
    private const long MaxImageBytes = 5 * 1024 * 1024;
    private const long MaxVideoBytes = 50 * 1024 * 1024;

    private readonly IProductReviewRepository _reviews;
    private readonly IOrderRepository _orders;
    private readonly IProductRepository _products;
    private readonly IUserRepository _users;
    private readonly IReviewMediaStorage _mediaStorage;
    private readonly IUnitOfWork _uow;

    public ProductReviewApplicationService(
        IProductReviewRepository reviews,
        IOrderRepository orders,
        IProductRepository products,
        IUserRepository users,
        IReviewMediaStorage mediaStorage,
        IUnitOfWork uow)
    {
        _reviews = reviews;
        _orders = orders;
        _products = products;
        _users = users;
        _mediaStorage = mediaStorage;
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

    public async Task<Result<ProductReviewResponse>> CreateAsync(long userId, long productId, CreateProductReviewRequest request, CancellationToken ct)
    {
        if (request.Stars < 1 || request.Stars > 5)
            return Result<ProductReviewResponse>.Fail("Stars must be between 1 and 5.", ApplicationErrorCode.Validation);
        if (NormalizeComment(request.Comment)?.Length > 1000)
            return Result<ProductReviewResponse>.Fail("Comment must be 1000 characters or fewer.", ApplicationErrorCode.Validation);

        var product = await _products.GetByIdAsync(productId, ct);
        if (product is null) return Result<ProductReviewResponse>.Fail("Product not found.", ApplicationErrorCode.ResourceNotFound);

        var hasPurchased = await _orders.HasUserPurchasedProductAsync(userId, productId, ct);
        if (!hasPurchased) return Result<ProductReviewResponse>.Fail("Forbidden.", ApplicationErrorCode.Forbidden);

        var existing = await _reviews.GetByProductAndUserAsync(productId, userId, ct);
        if (existing is not null) return Result<ProductReviewResponse>.Fail("Already reviewed.", ApplicationErrorCode.Validation);

        var mediaError = ValidateMediaSet(request.Media, []);
        if (mediaError is not null) return Result<ProductReviewResponse>.Fail(mediaError, ApplicationErrorCode.Validation);

        var user = await _users.GetByIdAsync(userId, ct);
        var uploaded = new List<StoredReviewMedia>();
        try
        {
            foreach (var file in request.Media)
                uploaded.Add(await _mediaStorage.UploadAsync(file, ct));

            var review = new ProductReview
            {
                ProductId = productId,
                UserId = userId,
                Stars = request.Stars,
                Comment = NormalizeComment(request.Comment),
                CreatedAt = DateTimeOffset.UtcNow,
                Media = uploaded.Select((media, index) => ToEntity(media, (short)(index + 1))).ToList()
            };

            _reviews.Add(review);
            await _uow.SaveChangesAsync(ct);

            return Result<ProductReviewResponse>.Ok(Map(review, user?.Username ?? string.Empty));
        }
        catch
        {
            await CleanupAsync(uploaded, ct);
            throw;
        }
    }

    public async Task<Result<ProductReviewResponse>> UpdateAsync(long userId, long reviewId, UpdateProductReviewRequest request, CancellationToken ct)
    {
        if (request.Stars < 1 || request.Stars > 5)
            return Result<ProductReviewResponse>.Fail("Stars must be between 1 and 5.", ApplicationErrorCode.Validation);
        if (NormalizeComment(request.Comment)?.Length > 1000)
            return Result<ProductReviewResponse>.Fail("Comment must be 1000 characters or fewer.", ApplicationErrorCode.Validation);

        var review = await _reviews.GetByIdAsync(reviewId, ct);
        if (review is null) return Result<ProductReviewResponse>.Fail("Review not found.", ApplicationErrorCode.ResourceNotFound);
        if (review.UserId != userId) return Result<ProductReviewResponse>.Fail("Forbidden.", ApplicationErrorCode.Forbidden);

        var existingMedia = review.Media.ToList();
        var keepIds = request.KeepMediaIds.Distinct().ToHashSet();
        if (keepIds.Any(id => existingMedia.All(media => media.Id != id)))
            return Result<ProductReviewResponse>.Fail("Invalid media selection.", ApplicationErrorCode.Validation);

        var keptMedia = existingMedia.Where(media => keepIds.Contains(media.Id)).ToList();
        var removedMedia = existingMedia.Where(media => !keepIds.Contains(media.Id)).ToList();
        var mediaError = ValidateMediaSet(request.NewMedia, keptMedia);
        if (mediaError is not null) return Result<ProductReviewResponse>.Fail(mediaError, ApplicationErrorCode.Validation);

        var user = await _users.GetByIdAsync(userId, ct);
        var uploaded = new List<StoredReviewMedia>();
        try
        {
            foreach (var file in request.NewMedia)
                uploaded.Add(await _mediaStorage.UploadAsync(file, ct));

            foreach (var media in removedMedia)
                review.Media.Remove(media);

            var nextSortOrder = review.Media.Count == 0
                ? (short)1
                : (short)(review.Media.Max(media => media.SortOrder) + 1);

            foreach (var media in uploaded)
                review.Media.Add(ToEntity(media, nextSortOrder++));

            review.Stars = request.Stars;
            review.Comment = NormalizeComment(request.Comment);
            review.UpdatedAt = DateTimeOffset.UtcNow;

            await _uow.SaveChangesAsync(ct);

            await CleanupAsync(removedMedia.Select(ToStoredMedia), ct);

            return Result<ProductReviewResponse>.Ok(Map(review, user?.Username ?? string.Empty));
        }
        catch
        {
            await CleanupAsync(uploaded, ct);
            throw;
        }
    }

    public async Task<Result> DeleteAsync(long userId, long reviewId, CancellationToken ct)
    {
        var review = await _reviews.GetByIdAsync(reviewId, ct);
        if (review is null) return Result.Fail("Review not found.", ApplicationErrorCode.ResourceNotFound);
        if (review.UserId != userId) return Result.Fail("Forbidden.", ApplicationErrorCode.Forbidden);

        var media = review.Media.Select(ToStoredMedia).ToList();
        _reviews.Remove(review);
        await _uow.SaveChangesAsync(ct);
        await CleanupAsync(media, ct);
        return Result.Ok();
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
            MapMedia(r.Media),
            r.CreatedAt,
            r.UpdatedAt
        )).ToList();
    }

    public async Task<Result> AdminDeleteAsync(long reviewId, CancellationToken ct)
    {
        var review = await _reviews.GetByIdAsync(reviewId, ct);
        if (review is null) return Result.Fail("Review not found.", ApplicationErrorCode.ResourceNotFound);

        var media = review.Media.Select(ToStoredMedia).ToList();
        _reviews.Remove(review);
        await _uow.SaveChangesAsync(ct);
        await CleanupAsync(media, ct);
        return Result.Ok();
    }

    private static ProductReviewResponse Map(ProductReview x, string username)
        => new(x.Id, x.ProductId, x.UserId, username, x.Stars, x.Comment, MapMedia(x.Media), x.CreatedAt, x.UpdatedAt);

    private static IReadOnlyList<ProductReviewMediaResponse> MapMedia(IEnumerable<ProductReviewMedia> media)
        => media
            .OrderBy(x => x.SortOrder)
            .Select(x => new ProductReviewMediaResponse(
                x.Id,
                x.MediaType.ToString().ToLowerInvariant(),
                x.Url,
                x.ThumbnailUrl,
                x.SortOrder))
            .ToList();

    private static ProductReviewMedia ToEntity(StoredReviewMedia media, short sortOrder)
        => new()
        {
            MediaType = media.MediaType,
            Url = media.Url,
            ThumbnailUrl = media.ThumbnailUrl,
            StoragePublicId = media.StoragePublicId,
            SortOrder = sortOrder,
            CreatedAt = DateTimeOffset.UtcNow
        };

    private static StoredReviewMedia ToStoredMedia(ProductReviewMedia media)
        => new(media.MediaType, media.Url, media.ThumbnailUrl, media.StoragePublicId);

    private static string? NormalizeComment(string? comment)
        => string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();

    private static string? ValidateMediaSet(
        IReadOnlyList<UploadFile> newMedia,
        IReadOnlyList<ProductReviewMedia> existingMedia)
    {
        var types = existingMedia.Select(x => x.MediaType).ToList();
        foreach (var file in newMedia)
        {
            var mediaType = GetMediaType(file.ContentType);
            if (mediaType is null)
                return "Only JPEG, PNG, WEBP, MP4, WEBM, or MOV files are allowed.";

            var maxBytes = mediaType == ReviewMediaType.Image ? MaxImageBytes : MaxVideoBytes;
            if (file.Length <= 0) return "Media file is empty.";
            if (file.Length > maxBytes)
                return mediaType == ReviewMediaType.Image
                    ? "Image files must be 5MB or smaller."
                    : "Video files must be 50MB or smaller.";

            types.Add(mediaType.Value);
        }

        if (types.Count > MaxMediaCount) return "A maximum of 6 media files is allowed per review.";
        if (types.Count(x => x == ReviewMediaType.Image) > MaxImageCount)
            return "A maximum of 5 images is allowed per review.";
        if (types.Count(x => x == ReviewMediaType.Video) > MaxVideoCount)
            return "A maximum of 1 video is allowed per review.";

        return null;
    }

    private static ReviewMediaType? GetMediaType(string? contentType)
        => contentType?.ToLowerInvariant() switch
        {
            "image/jpeg" or "image/png" or "image/webp" => ReviewMediaType.Image,
            "video/mp4" or "video/webm" or "video/quicktime" => ReviewMediaType.Video,
            _ => null
        };

    private async Task CleanupAsync(IEnumerable<StoredReviewMedia> media, CancellationToken ct)
    {
        foreach (var item in media)
        {
            try
            {
                await _mediaStorage.DeleteAsync(item.StoragePublicId, item.MediaType, CancellationToken.None);
            }
            catch
            {
                // Storage adapters log cleanup failures with the public ID. DB state remains authoritative.
            }
        }
    }
}
