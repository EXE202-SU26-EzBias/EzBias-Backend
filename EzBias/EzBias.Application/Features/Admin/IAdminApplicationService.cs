using EzBias.Application.Common.Results;
using EzBias.Application.Features.Admin.Dtos;

namespace EzBias.Application.Features.Admin;

public interface IAdminApplicationService
{
    Task<AdminDashboardOverviewResponse> GetDashboardOverviewAsync(CancellationToken ct);
    Task<AdminUserListResponse> GetUsersAsync(AdminUserListQuery query, CancellationToken ct);
    Task<Result<AdminUserDetailResponse>> GetUserDetailAsync(long userId, CancellationToken ct);
    Task<Result<AdminUserDetailResponse>> CreateUserAsync(AdminCreateUserRequest request, CancellationToken ct);
    Task<Result<AdminUserDetailResponse>> UpdateUserAsync(long userId, AdminUpdateUserRequest request, CancellationToken ct);
    Task<Result> SoftDeleteUserAsync(long userId, long adminId, CancellationToken ct);
    Task<Result<AdminUserDetailResponse>> RestoreUserAsync(long userId, CancellationToken ct);
    Task<IReadOnlyList<AdminTransactionItem>> GetTransactionsAsync(CancellationToken ct);
}
