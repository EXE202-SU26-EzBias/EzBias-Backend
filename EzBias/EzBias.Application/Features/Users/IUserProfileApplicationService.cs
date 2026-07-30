using EzBias.Application.Common.Results;
using EzBias.Application.Features.Users.Dtos;

namespace EzBias.Application.Features.Users;

public interface IUserProfileApplicationService
{
    Task<Result<UserProfileResponse>> GetMeAsync(long userId, CancellationToken ct);
    Task<Result<UserProfileResponse>> UpdateMeAsync(long userId, UpdateUserProfileRequest request, CancellationToken ct);
    Task<Result> DeleteUnverifiedByEmailAsync(string email, CancellationToken ct);
    Task<SellerDashboardResponse> GetSellerDashboardAsync(long sellerId, CancellationToken ct);
}
