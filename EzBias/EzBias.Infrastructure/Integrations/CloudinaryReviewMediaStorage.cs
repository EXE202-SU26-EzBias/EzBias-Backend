using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using EzBias.Application.Features.Reviews;
using EzBias.Application.Features.Reviews.Dtos;
using EzBias.Application.Features.Media;
using EzBias.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EzBias.Infrastructure.Integrations;

public sealed class CloudinaryReviewMediaStorage : IReviewMediaStorage
{
    private static readonly HashSet<string> AllowedImageTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    private static readonly HashSet<string> AllowedVideoTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "video/mp4",
        "video/webm",
        "video/quicktime"
    };

    private const long MaxImageBytes = 5 * 1024 * 1024;
    private const long MaxVideoBytes = 50 * 1024 * 1024;
    private const double MaxVideoDurationSeconds = 60;

    private readonly Cloudinary _cloudinary;
    private readonly CloudinaryOptions _options;
    private readonly ILogger<CloudinaryReviewMediaStorage> _logger;

    public CloudinaryReviewMediaStorage(
        IOptions<CloudinaryOptions> options,
        ILogger<CloudinaryReviewMediaStorage> logger)
    {
        _options = options.Value;
        _logger = logger;
        var account = new Account(_options.CloudName, _options.ApiKey, _options.ApiSecret);
        _cloudinary = new Cloudinary(account) { Api = { Secure = true } };
    }

    public async Task<StoredReviewMedia> UploadAsync(UploadFile file, CancellationToken ct)
    {
        if (!_options.IsConfigured)
            throw new InvalidOperationException("Cloudinary is not configured.");

        var mediaType = GetMediaType(file.ContentType);
        if (mediaType is null)
            throw new InvalidOperationException("Only JPEG, PNG, WEBP, MP4, WEBM, or MOV files are allowed.");

        var maxBytes = mediaType == ReviewMediaType.Image ? MaxImageBytes : MaxVideoBytes;
        if (file.Length <= 0)
            throw new InvalidOperationException("Media file is empty.");
        if (file.Length > maxBytes)
            throw new InvalidOperationException(mediaType == ReviewMediaType.Image
                ? "Image files must be 5MB or smaller."
                : "Video files must be 50MB or smaller.");

        var stream = file.Content;
        UploadResult result;

        if (mediaType == ReviewMediaType.Image)
        {
            result = await _cloudinary.UploadAsync(new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = _options.ReviewFolder,
                UseFilename = false,
                UniqueFilename = true,
                Overwrite = false
            }, ct);
        }
        else
        {
            var videoResult = await _cloudinary.UploadAsync(new VideoUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = _options.ReviewFolder,
                UseFilename = false,
                UniqueFilename = true,
                Overwrite = false
            }, ct);

            if (videoResult.Error is not null)
                throw new InvalidOperationException($"Cloudinary upload failed: {videoResult.Error.Message}");

            if (videoResult.Duration > MaxVideoDurationSeconds)
            {
                await TryDeleteAsync(videoResult.PublicId, ReviewMediaType.Video, CancellationToken.None);
                throw new InvalidOperationException("Videos must be 60 seconds or shorter.");
            }

            result = videoResult;
        }

        if (result.Error is not null)
            throw new InvalidOperationException($"Cloudinary upload failed: {result.Error.Message}");

        var mediaUrl = result.SecureUrl.ToString();
        return new StoredReviewMedia(
            mediaType.Value,
            mediaUrl,
            mediaType == ReviewMediaType.Video ? BuildVideoThumbnailUrl(mediaUrl) : null,
            result.PublicId);
    }

    public Task DeleteAsync(string publicId, ReviewMediaType mediaType, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return TryDeleteAsync(publicId, mediaType, ct);
    }

    private async Task TryDeleteAsync(string publicId, ReviewMediaType mediaType, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(publicId) || !_options.IsConfigured)
            return;

        try
        {
            var result = await _cloudinary.DestroyAsync(new DeletionParams(publicId)
            {
                ResourceType = mediaType == ReviewMediaType.Video ? ResourceType.Video : ResourceType.Image,
                Type = "upload",
                Invalidate = true
            });

            if (result.Error is not null)
                throw new InvalidOperationException(result.Error.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Failed to delete review media asset {CloudinaryPublicId}", publicId);
        }
    }

    private static ReviewMediaType? GetMediaType(string? contentType)
    {
        if (contentType is not null && AllowedImageTypes.Contains(contentType))
            return ReviewMediaType.Image;
        if (contentType is not null && AllowedVideoTypes.Contains(contentType))
            return ReviewMediaType.Video;
        return null;
    }

    private static string BuildVideoThumbnailUrl(string videoUrl)
    {
        var queryIndex = videoUrl.IndexOf('?');
        var pathEnd = queryIndex >= 0 ? queryIndex : videoUrl.Length;
        var extensionIndex = videoUrl.LastIndexOf('.', pathEnd - 1);
        var slashIndex = videoUrl.LastIndexOf('/', pathEnd - 1);
        if (extensionIndex <= slashIndex)
            return $"{videoUrl[..pathEnd]}.jpg{videoUrl[pathEnd..]}";

        return $"{videoUrl[..extensionIndex]}.jpg{videoUrl[pathEnd..]}";
    }
}
