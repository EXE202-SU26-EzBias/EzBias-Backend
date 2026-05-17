using EzBias.Application.Features.Admin.Dtos;

namespace EzBias.Application.Features.Admin;

public interface IAdminApplicationService
{
    Task<AdminDashboardOverviewResponse> GetDashboardOverviewAsync(CancellationToken ct);
    Task<AdminUserListResponse> GetUsersAsync(AdminUserListQuery query, CancellationToken ct);
    Task<(bool Success, string? Error, AdminUserDetailResponse? Data)> GetUserDetailAsync(long userId, CancellationToken ct);
}
