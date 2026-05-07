using EzBias.Application.Features.Users.Dtos;

namespace EzBias.Application.Features.Users;

public interface IUserProfileApplicationService
{
    Task<(bool Success, string? Error, UserProfileResponse? Data)> GetMeAsync(long userId, CancellationToken ct);
    Task<(bool Success, string? Error, UserProfileResponse? Data)> UpdateMeAsync(long userId, UpdateUserProfileRequest request, CancellationToken ct);
}
