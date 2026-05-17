using EzBias.Application.Features.Admin.Dtos;

namespace EzBias.Application.Features.Admin;

public interface IAdminApplicationService
{
    Task<AdminDashboardOverviewResponse> GetDashboardOverviewAsync(CancellationToken ct);
    Task<AdminUserListResponse> GetUsersAsync(AdminUserListQuery query, CancellationToken ct);
    Task<(bool Success, string? Error, AdminUserDetailResponse? Data)> GetUserDetailAsync(long userId, CancellationToken ct);
    Task<(bool Success, string? Error, AdminUserDetailResponse? Data)> CreateUserAsync(AdminCreateUserRequest request, CancellationToken ct);
    Task<(bool Success, string? Error, AdminUserDetailResponse? Data)> UpdateUserAsync(long userId, AdminUpdateUserRequest request, CancellationToken ct);
    Task<(bool Success, string? Error)> SoftDeleteUserAsync(long userId, long adminId, CancellationToken ct);
    Task<(bool Success, string? Error, AdminUserDetailResponse? Data)> RestoreUserAsync(long userId, CancellationToken ct);
}
