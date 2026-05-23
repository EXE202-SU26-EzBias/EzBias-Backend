using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;

namespace EzBias.API.Integrations;

public sealed class CloudinaryImageUploader : IImageUploader
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    private readonly Cloudinary _cloudinary;
    private readonly CloudinaryOptions _options;

    public CloudinaryImageUploader(IOptions<CloudinaryOptions> options)
    {
        _options = options.Value;
        var account = new Account(_options.CloudName, _options.ApiKey, _options.ApiSecret);
        _cloudinary = new Cloudinary(account) { Api = { Secure = true } };
    }

    public async Task<string> UploadProductImageAsync(IFormFile file, CancellationToken ct)
    {
        if (!_options.IsConfigured)
            throw new InvalidOperationException("Cloudinary is not configured.");

        if (file.Length <= 0)
            throw new InvalidOperationException("Image file is empty.");

        if (file.Length > 5 * 1024 * 1024)
            throw new InvalidOperationException("Image file must be 5MB or smaller.");

        if (!AllowedContentTypes.Contains(file.ContentType))
            throw new InvalidOperationException("Only JPEG, PNG, or WEBP images are allowed.");

        await using var stream = file.OpenReadStream();

        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Folder = _options.Folder,
            UseFilename = false,
            UniqueFilename = true,
            Overwrite = false
        };

        var result = await _cloudinary.UploadAsync(uploadParams, ct);

        if (result.Error is not null)
            throw new InvalidOperationException($"Cloudinary upload failed: {result.Error.Message}");

        return result.SecureUrl.ToString();
    }
}
