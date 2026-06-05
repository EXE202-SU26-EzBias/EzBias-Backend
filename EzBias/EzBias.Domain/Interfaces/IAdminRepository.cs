using EzBias.Domain.Entities;
using EzBias.Domain.Enums;

namespace EzBias.Domain.Interfaces;

public interface IAdminRepository
{
    Task<AdminDashboardOverviewData> GetDashboardOverviewAsync(CancellationToken ct);
    Task<(IReadOnlyList<User> Items, int TotalItems)> GetUsersAsync(string? keyword, UserRole? role, bool? isDeleted, int page, int pageSize, CancellationToken ct);
    Task<User?> GetUserDetailAsync(long userId, CancellationToken ct);
    Task<User?> GetUserByIdAsync(long userId, CancellationToken ct);
    Task<bool> ExistsByEmailOrUsernameAsync(string normalizedEmail, string normalizedUsername, long? excludeUserId, CancellationToken ct);
    void AddUser(User user);
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
    decimal TotalCommissionRevenue,
    decimal CommissionRevenueToday,
    decimal CommissionRevenueLast7Days,
    decimal CommissionRevenueLast30Days,
    int OpenDisputes,
    int PendingRefunds,
    int PendingPayouts,
    IReadOnlyList<AdminTopSellerCommissionData> TopSellersByNetRevenue,
    IReadOnlyList<AdminMonthlySalesData> MonthlySales
);

public record AdminMonthlySalesData(
    string Month,
    string Label,
    int OrderCount,
    decimal GrossSales,
    decimal CommissionRevenue,
    decimal SellerNetAmount
);

public record AdminTopSellerCommissionData(
    long SellerId,
    string Username,
    string FullName,
    int OrderCount,
    decimal GrossRevenue,
    decimal CommissionRevenue,
    decimal NetRevenue
);
