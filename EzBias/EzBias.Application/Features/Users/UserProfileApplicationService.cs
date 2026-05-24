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

        // Commission
        var payouts = await _payouts.GetBySellerAsync(sellerId, null, ct);
        var pendingPayouts = payouts.Where(p => p.Status == PayoutStatus.Pending || p.Status == PayoutStatus.Processing).ToList();
        var paidPayouts = payouts.Where(p => p.Status == PayoutStatus.Paid).ToList();

        // Revenue from completed orders via commission transactions
        var completedOrders = sellerOrders.Where(o => o.Status == OrderStatus.Completed).ToList();
        var grossRevenue = completedOrders.Sum(o => o.Total);

        // Sum commission paid from payout amounts (net = payout amount, commission = gross - net)
        var netRevenue = paidPayouts.Sum(p => p.Amount);
        var commissionPaid = grossRevenue - netRevenue < 0 ? 0 : grossRevenue - netRevenue;

        // Auctions
        var allAuctions = await _auctions.GetBySellerAsync(sellerId, null, ct);

        return new SellerDashboardResponse(
            GrossRevenue: grossRevenue,
            CommissionPaid: commissionPaid,
            NetRevenue: netRevenue,
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
            TotalRatings: user?.TotalRatings ?? 0
        );
    }

    private static UserProfileResponse Map(Domain.Entities.User user)
        => new(user.Id, user.FullName, user.Username, user.Email, user.Phone, user.Address, user.City, user.Zip, user.AvatarUrl, user.AvatarBg, user.BankName, user.BankAccountNumber, user.BankAccountName);
}
