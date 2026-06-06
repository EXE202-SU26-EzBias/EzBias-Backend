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
        var totalCommissionRevenue = await _db.CommissionTransactions.SumAsync(x => (decimal?)x.CommissionAmount, ct) ?? 0m;
        var commissionRevenueToday = await _db.CommissionTransactions
            .Where(x => x.CreatedAt >= todayStart)
            .SumAsync(x => (decimal?)x.CommissionAmount, ct) ?? 0m;
        var commissionRevenueLast7Days = await _db.CommissionTransactions
            .Where(x => x.CreatedAt >= last7Days)
            .SumAsync(x => (decimal?)x.CommissionAmount, ct) ?? 0m;
        var commissionRevenueLast30Days = await _db.CommissionTransactions
            .Where(x => x.CreatedAt >= last30Days)
            .SumAsync(x => (decimal?)x.CommissionAmount, ct) ?? 0m;

        var topSellerStats = await _db.CommissionTransactions
            .GroupBy(x => x.SellerId)
            .Select(g => new
            {
                SellerId = g.Key,
                OrderCount = g.Count(),
                GrossRevenue = g.Sum(x => x.GrossAmount),
                CommissionRevenue = g.Sum(x => x.CommissionAmount),
                NetRevenue = g.Sum(x => x.SellerNetAmount)
            })
            .OrderByDescending(x => x.NetRevenue)
            .Take(5)
            .ToListAsync(ct);

        var topSellerIds = topSellerStats.Select(x => x.SellerId).ToList();
        var topSellerProfiles = await _db.Users
            .Where(x => topSellerIds.Contains(x.Id))
            .Select(x => new { x.Id, x.Username, x.FullName })
            .ToListAsync(ct);

        var topSellers = topSellerStats.Select(x =>
        {
            var seller = topSellerProfiles.FirstOrDefault(s => s.Id == x.SellerId);
            return new AdminTopSellerCommissionData(
                x.SellerId,
                seller?.Username ?? string.Empty,
                seller?.FullName ?? string.Empty,
                x.OrderCount,
                x.GrossRevenue,
                x.CommissionRevenue,
                x.NetRevenue);
        }).ToList();

        var openDisputes = await _db.Disputes.CountAsync(x => x.Status == DisputeStatus.Open || x.Status == DisputeStatus.UnderReview, ct);
        var pendingRefunds = await _db.Refunds.CountAsync(x => x.Status == RefundStatus.Pending, ct);
        var pendingPayouts = await _db.Payouts.CountAsync(x => x.Status == PayoutStatus.Pending, ct);

        var monthlySales = await BuildMonthlySalesAsync(now, ct);

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
            totalCommissionRevenue,
            commissionRevenueToday,
            commissionRevenueLast7Days,
            commissionRevenueLast30Days,
            openDisputes,
            pendingRefunds,
            pendingPayouts,
            topSellers,
            monthlySales);
    }

    private static readonly string[] _monthAbbr = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

    // Last-12-calendar-months commission rollup (oldest first), dense so zero-sale months still render.
    private async Task<IReadOnlyList<AdminMonthlySalesData>> BuildMonthlySalesAsync(DateTimeOffset now, CancellationToken ct)
    {
        var windowStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero).AddMonths(-11);

        var raw = await _db.CommissionTransactions
            .Where(x => x.CreatedAt >= windowStart)
            .GroupBy(x => new { x.CreatedAt.Year, x.CreatedAt.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                OrderCount = g.Count(),
                GrossSales = g.Sum(x => x.GrossAmount),
                CommissionRevenue = g.Sum(x => x.CommissionAmount),
                SellerNetAmount = g.Sum(x => x.SellerNetAmount)
            })
            .ToListAsync(ct);

        var byKey = raw.ToDictionary(x => (x.Year, x.Month));

        var points = new List<AdminMonthlySalesData>(12);
        for (var i = 11; i >= 0; i--)
        {
            var d = now.AddMonths(-i);
            var monthKey = $"{d.Year:D4}-{d.Month:D2}";
            var label = $"{_monthAbbr[d.Month - 1]} {d.Year}";

            if (byKey.TryGetValue((d.Year, d.Month), out var r))
                points.Add(new AdminMonthlySalesData(monthKey, label, r.OrderCount, r.GrossSales, r.CommissionRevenue, r.SellerNetAmount));
            else
                points.Add(new AdminMonthlySalesData(monthKey, label, 0, 0m, 0m, 0m));
        }

        return points;
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

    public Task<User?> GetUserByIdAsync(long userId, CancellationToken ct)
        => _db.Users.FirstOrDefaultAsync(x => x.Id == userId, ct);

    public Task<bool> ExistsByEmailOrUsernameAsync(string normalizedEmail, string normalizedUsername, long? excludeUserId, CancellationToken ct)
        => _db.Users.AnyAsync(x =>
            (excludeUserId == null || x.Id != excludeUserId.Value)
            && (x.Email.ToLower() == normalizedEmail || x.Username.ToLower() == normalizedUsername), ct);

    public void AddUser(User user) => _db.Users.Add(user);
}
