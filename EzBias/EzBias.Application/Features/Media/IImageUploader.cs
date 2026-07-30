namespace EzBias.Application.Features.Media;

public interface IImageUploader
{
    Task<string> UploadProductImageAsync(UploadFile file, CancellationToken ct);
}

public sealed record UploadFile(
    Stream Content,
    string FileName,
    string ContentType,
    long Length);
