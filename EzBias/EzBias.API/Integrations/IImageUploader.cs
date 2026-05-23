namespace EzBias.API.Integrations;

public interface IImageUploader
{
    Task<string> UploadProductImageAsync(IFormFile file, CancellationToken ct);
}
