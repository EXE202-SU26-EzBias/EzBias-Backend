using EzBias.Application.Features.Admin.Dtos;
using EzBias.Domain.Enums;
using EzBias.Domain.Interfaces;

namespace EzBias.Application.Features.Admin;

public class AdminApplicationService : IAdminApplicationService
{
    private readonly IAdminRepository _adminRepository;

    public AdminApplicationService(IAdminRepository adminRepository)
    {
        _adminRepository = adminRepository;
    }

    public async Task<AdminDashboardOverviewResponse> GetDashboardOverviewAsync(CancellationToken ct)
    {
        var x = await _adminRepository.GetDashboardOverviewAsync(ct);
        return new AdminDashboardOverviewResponse(x.TotalUsers, x.NewUsersToday, x.NewUsersLast7Days, x.NewUsersLast30Days, x.TotalOrders, x.PendingOrders, x.PaidOrders, x.ProcessingOrders, x.ShippedOrders, x.DeliveredOrders, x.ReturnRequestedOrders, x.CompletedOrders, x.CanceledOrders, x.RefundedOrders, x.GrossRevenue, x.RefundedAmount, x.NetRevenue, x.OpenDisputes, x.PendingRefunds, x.PendingPayouts);
    }

    public async Task<AdminUserListResponse> GetUsersAsync(AdminUserListQuery query, CancellationToken ct)
    {
        var safePage = query.Page <= 0 ? 1 : query.Page;
        var safePageSize = query.PageSize <= 0 ? 20 : Math.Min(query.PageSize, 100);

        UserRole? role = null;
        if (!string.IsNullOrWhiteSpace(query.Role) && Enum.TryParse<UserRole>(query.Role, true, out var parsedRole))
            role = parsedRole;

        var (items, totalItems) = await _adminRepository.GetUsersAsync(query.Keyword, role, query.IsDeleted, safePage, safePageSize, ct);
        var totalPages = (int)Math.Ceiling(totalItems / (double)safePageSize);

        var mapped = items.Select(x => new AdminUserListItemResponse(x.Id, x.FullName, x.Username, x.Email, x.Role.ToString(), x.DeletedAt != null, x.CreatedAt)).ToList();
        return new AdminUserListResponse(mapped, safePage, safePageSize, totalItems, totalPages);
    }

    public async Task<(bool Success, string? Error, AdminUserDetailResponse? Data)> GetUserDetailAsync(long userId, CancellationToken ct)
    {
        var user = await _adminRepository.GetUserDetailAsync(userId, ct);
        if (user is null) return (false, "User not found.", null);
        var data = new AdminUserDetailResponse(user.Id, user.FullName, user.Username, user.Email, user.Role.ToString(), user.Phone, user.City, user.DeletedAt != null, user.CreatedAt, user.UpdatedAt, user.OrdersAsBuyer.Count, user.OrdersAsSeller.Count, user.DisputesOpened.Count, user.AvgSellerRating, user.TotalRatings);
        return (true, null, data);
    }
}
