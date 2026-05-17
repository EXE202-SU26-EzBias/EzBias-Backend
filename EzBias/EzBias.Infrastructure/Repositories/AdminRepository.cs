using EzBias.Domain.Entities;
using EzBias.Domain.Enums;
using EzBias.Domain.Interfaces;
using EzBias.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EzBias.Infrastructure.Repositories;

public class AdminRepository : IAdminRepository
{
    private readonly EzBiasDbContext _db;

    public AdminRepository(EzBiasDbContext db)
    {
        _db = db;
    }

    public async Task<AdminDashboardOverviewData> GetDashboardOverviewAsync(CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var todayStart = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, TimeSpan.Zero);
        var last7Days = now.AddDays(-7);
        var last30Days = now.AddDays(-30);

        var totalUsers = await _db.Users.CountAsync(ct);
        var newUsersToday = await _db.Users.CountAsync(x => x.CreatedAt >= todayStart, ct);
        var newUsersLast7Days = await _db.Users.CountAsync(x => x.CreatedAt >= last7Days, ct);
        var newUsersLast30Days = await _db.Users.CountAsync(x => x.CreatedAt >= last30Days, ct);

        var totalOrders = await _db.Orders.CountAsync(ct);
        var orderStatusCounts = await _db.Orders
            .GroupBy(x => x.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        int Count(OrderStatus status) => orderStatusCounts.FirstOrDefault(x => x.Status == status)?.Count ?? 0;

        var grossRevenue = await _db.Payments.Where(x => x.Status == PaymentStatus.Paid).SumAsync(x => (decimal?)x.Amount, ct) ?? 0m;
        var refundedAmount = await _db.Refunds.Where(x => x.Status == RefundStatus.Processed).SumAsync(x => (decimal?)x.Amount, ct) ?? 0m;
        var netRevenue = grossRevenue - refundedAmount;

        var openDisputes = await _db.Disputes.CountAsync(x => x.Status == DisputeStatus.Open || x.Status == DisputeStatus.UnderReview, ct);
        var pendingRefunds = await _db.Refunds.CountAsync(x => x.Status == RefundStatus.Pending, ct);
        var pendingPayouts = await _db.Payouts.CountAsync(x => x.Status == PayoutStatus.Pending || x.Status == PayoutStatus.Processing, ct);

        return new AdminDashboardOverviewData(
            totalUsers,
            newUsersToday,
            newUsersLast7Days,
            newUsersLast30Days,
            totalOrders,
            Count(OrderStatus.Pending),
            Count(OrderStatus.Paid),
            Count(OrderStatus.Processing),
            Count(OrderStatus.Shipped),
            Count(OrderStatus.Delivered),
            Count(OrderStatus.ReturnRequested),
            Count(OrderStatus.Completed),
            Count(OrderStatus.Canceled),
            Count(OrderStatus.Refunded),
            grossRevenue,
            refundedAmount,
            netRevenue,
            openDisputes,
            pendingRefunds,
            pendingPayouts);
    }

    public async Task<(IReadOnlyList<User> Items, int TotalItems)> GetUsersAsync(string? keyword, UserRole? role, bool? isDeleted, int page, int pageSize, CancellationToken ct)
    {
        IQueryable<User> query = _db.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var key = keyword.Trim().ToLower();
            query = query.Where(x => x.Email.ToLower().Contains(key) || x.Username.ToLower().Contains(key) || x.FullName.ToLower().Contains(key));
        }

        if (role.HasValue)
            query = query.Where(x => x.Role == role.Value);

        if (isDeleted.HasValue)
            query = isDeleted.Value ? query.Where(x => x.DeletedAt != null) : query.Where(x => x.DeletedAt == null);

        var totalItems = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalItems);
    }

    public async Task<User?> GetUserDetailAsync(long userId, CancellationToken ct)
    {
        return await _db.Users
            .AsNoTracking()
            .Include(x => x.OrdersAsBuyer)
            .Include(x => x.OrdersAsSeller)
            .Include(x => x.DisputesOpened)
            .FirstOrDefaultAsync(x => x.Id == userId, ct);
    }
}
