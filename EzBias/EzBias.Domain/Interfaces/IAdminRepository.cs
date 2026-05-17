using EzBias.Domain.Entities;
using EzBias.Domain.Enums;

namespace EzBias.Domain.Interfaces;

public interface IAdminRepository
{
    Task<AdminDashboardOverviewData> GetDashboardOverviewAsync(CancellationToken ct);
    Task<(IReadOnlyList<User> Items, int TotalItems)> GetUsersAsync(string? keyword, UserRole? role, bool? isDeleted, int page, int pageSize, CancellationToken ct);
    Task<User?> GetUserDetailAsync(long userId, CancellationToken ct);
}

public record AdminDashboardOverviewData(
    int TotalUsers,
    int NewUsersToday,
    int NewUsersLast7Days,
    int NewUsersLast30Days,
    int TotalOrders,
    int PendingOrders,
    int PaidOrders,
    int ProcessingOrders,
    int ShippedOrders,
    int DeliveredOrders,
    int ReturnRequestedOrders,
    int CompletedOrders,
    int CanceledOrders,
    int RefundedOrders,
    decimal GrossRevenue,
    decimal RefundedAmount,
    decimal NetRevenue,
    int OpenDisputes,
    int PendingRefunds,
    int PendingPayouts
);