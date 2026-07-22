using EzBias.Application.Features.Reviews.Dtos;
using EzBias.Domain.Enums;

namespace EzBias.Application.Features.Reviews;

public interface IReviewMediaStorage
{
    Task<StoredReviewMedia> UploadAsync(ReviewMediaFile file, CancellationToken ct);
    Task DeleteAsync(string publicId, ReviewMediaType mediaType, CancellationToken ct);
}
