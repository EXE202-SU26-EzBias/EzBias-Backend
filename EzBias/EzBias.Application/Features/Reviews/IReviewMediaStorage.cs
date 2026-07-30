using EzBias.Application.Features.Media;
using EzBias.Application.Features.Reviews.Dtos;
using EzBias.Domain.Enums;

namespace EzBias.Application.Features.Reviews;

public interface IReviewMediaStorage
{
    Task<StoredReviewMedia> UploadAsync(UploadFile file, CancellationToken ct);
    Task DeleteAsync(string storagePublicId, ReviewMediaType mediaType, CancellationToken ct);
}
