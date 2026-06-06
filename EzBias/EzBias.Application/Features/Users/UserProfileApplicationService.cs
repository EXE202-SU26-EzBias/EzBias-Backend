using EzBias.Application.Features.Users.Dtos;
using EzBias.Domain.Enums;
using EzBias.Domain.Interfaces;

namespace EzBias.Application.Features.Users;

public class UserProfileApplicationService : IUserProfileApplicationService
{
    private readonly IUserRepository _users;
    private readonly IOrderRepository _orders;
    private readonly ICommissionRepository _commissions;
    private readonly IPayoutRepository _payouts;
    private readonly IAuctionRepository _auctions;
    private readonly IUnitOfWork _uow;

    public UserProfileApplicationService(
        IUserRepository users,
        IOrderRepository orders,
        ICommissionRepository commissions,
        IPayoutRepository payouts,
        IAuctionRepository auctions,
        IUnitOfWork uow)
    {
        _users = users;
        _orders = orders;
        _commissions = commissions;
        _payouts = payouts;
        _auctions = auctions;
        _uow = uow;
    }

    public async Task<(bool Success, string? Error, UserProfileResponse? Data)> GetMeAsync(long userId, CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null) return (false, "User not found.", null);
        return (true, null, Map(user));
    }

    public async Task<(bool Success, string? Error, UserProfileResponse? Data)> UpdateMeAsync(long userId, UpdateUserProfileRequest request, CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null) return (false, "User not found.", null);

        user.FullName = request.FullName?.Trim() ?? string.Empty;
        user.Phone = request.Phone?.Trim() ?? string.Empty;
        user.Address = request.Address?.Trim() ?? string.Empty;
        user.City = request.City?.Trim() ?? string.Empty;
        user.Zip = request.Zip?.Trim() ?? string.Empty;
        user.BankName = request.BankName?.Trim() ?? string.Empty;
        user.BankAccountNumber = request.BankAccountNumber?.Trim() ?? string.Empty;
        user.BankAccountName = request.BankAccountName?.Trim() ?? string.Empty;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        await _uow.SaveChangesAsync(ct);
        return (true, null, Map(user));
    }

    public async Task<(bool Success, string? Error)> DeleteUnverifiedByEmailAsync(string email, CancellationToken ct)
    {
        var normalizedEmail = (email ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalizedEmail))
            return (false, "Email is required.");

        var user = await _users.GetByEmailAsync(normalizedEmail, ct);
        if (user is null)
            return (false, "User not found.");

        if (user.EmailVerifiedAt is not null)
            return (false, "Only unverified users can be deleted.");

        _users.Remove(user);
        await _uow.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task<SellerDashboardResponse> GetSellerDashboardAsync(long sellerId, CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(sellerId, ct);

        // Orders
        var sellerOrders = await _orders.GetBySellerAsync(sellerId, ct);
        int Count(OrderStatus s) => sellerOrders.Count(o => o.Status == s);

        // Payouts
        var payouts = await _payouts.GetBySellerAsync(sellerId, null, ct);
        var pendingPayouts = payouts.Where(p => p.Status == PayoutStatus.Pending).ToList();
        var paidPayouts = payouts.Where(p => p.Status == PayoutStatus.Approved).ToList();

        // Revenue + items sold come from commission transactions (written when an order is Paid)
        // — the authoritative record of realized sales, gross/commission/net per order.
        var commissions = await _commissions.GetBySellerWithItemsAsync(sellerId, null, ct);
        var grossRevenue = commissions.Sum(c => c.GrossAmount);
        var commissionPaid = commissions.Sum(c => c.CommissionAmount);
        var netRevenue = commissions.Sum(c => c.SellerNetAmount);
        var itemsSold = commissions.Sum(c => c.Order?.Items.Sum(i => i.Quantity) ?? 0);

        var monthlySales = BuildMonthlySeries(commissions);

        // Auctions
        var allAuctions = await _auctions.GetBySellerAsync(sellerId, null, ct);

        return new SellerDashboardResponse(
            GrossRevenue: grossRevenue,
            CommissionPaid: commissionPaid,
            NetRevenue: netRevenue,
            ItemsSold: itemsSold,
            TotalOrders: sellerOrders.Count,
            PendingOrders: Count(OrderStatus.Pending),
            PaidOrders: Count(OrderStatus.Paid),
            ShippedOrders: Count(OrderStatus.Shipped),
            DeliveredOrders: Count(OrderStatus.Delivered),
            CompletedOrders: Count(OrderStatus.Completed),
            CanceledOrders: Count(OrderStatus.Canceled),
            PendingPayouts: pendingPayouts.Count,
            PaidPayouts: paidPayouts.Count,
            PendingPayoutAmount: pendingPayouts.Sum(p => p.Amount),
            PaidPayoutAmount: paidPayouts.Sum(p => p.Amount),
            TotalAuctions: allAuctions.Count,
            LiveAuctions: allAuctions.Count(a => a.Status == AuctionStatus.Live || a.Status == AuctionStatus.Extended),
            SoldAuctions: allAuctions.Count(a => a.Status == AuctionStatus.Sold),
            AvgRating: user?.AvgSellerRating ?? 0m,
            TotalRatings: user?.TotalRatings ?? 0,
            MonthlySales: monthlySales
        );
    }

    // Builds a dense last-12-calendar-months series (oldest first) so the chart has no gaps,
    // even for months with zero sales. Buckets by commission CreatedAt (UTC).
    private static readonly string[] _monthAbbr = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

    private static IReadOnlyList<SellerMonthlySalesPoint> BuildMonthlySeries(IReadOnlyList<Domain.Entities.CommissionTransaction> commissions)
    {
        var now = DateTimeOffset.UtcNow;
        var buckets = commissions
            .GroupBy(c => new { c.CreatedAt.Year, c.CreatedAt.Month })
            .ToDictionary(g => (g.Key.Year, g.Key.Month), g => g.ToList());

        var points = new List<SellerMonthlySalesPoint>(12);
        for (var i = 11; i >= 0; i--)
        {
            var d = now.AddMonths(-i);
            var key = (d.Year, d.Month);
            var monthKey = $"{d.Year:D4}-{d.Month:D2}";
            var label = $"{_monthAbbr[d.Month - 1]} {d.Year}";

            if (buckets.TryGetValue(key, out var rows))
            {
                points.Add(new SellerMonthlySalesPoint(
                    monthKey,
                    label,
                    rows.Sum(c => c.Order?.Items.Sum(it => it.Quantity) ?? 0),
                    rows.Count,
                    rows.Sum(c => c.GrossAmount),
                    rows.Sum(c => c.CommissionAmount),
                    rows.Sum(c => c.SellerNetAmount)));
            }
            else
            {
                points.Add(new SellerMonthlySalesPoint(monthKey, label, 0, 0, 0m, 0m, 0m));
            }
        }

        return points;
    }

    private static UserProfileResponse Map(Domain.Entities.User user)
        => new(user.Id, user.FullName, user.Username, user.Email, user.Phone, user.Address, user.City, user.Zip, user.AvatarUrl, user.AvatarBg, user.BankName, user.BankAccountNumber, user.BankAccountName);
}
