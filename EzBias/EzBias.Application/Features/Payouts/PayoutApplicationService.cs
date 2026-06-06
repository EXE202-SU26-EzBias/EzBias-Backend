using EzBias.Application.Features.Notifications;
using EzBias.Application.Features.Payouts.Dtos;
using EzBias.Domain.Enums;
using EzBias.Domain.Interfaces;

namespace EzBias.Application.Features.Payouts;

public class PayoutApplicationService : IPayoutApplicationService
{
    private readonly IPayoutRepository _payouts;
    private readonly INotificationRepository _notifications;
    private readonly INotificationFactory _notificationFactory;
    private readonly IUnitOfWork _uow;

    public PayoutApplicationService(
        IPayoutRepository payouts,
        INotificationRepository notifications,
        INotificationFactory notificationFactory,
        IUnitOfWork uow)
    {
        _payouts = payouts;
        _notifications = notifications;
        _notificationFactory = notificationFactory;
        _uow = uow;
    }

    public async Task<IReadOnlyList<SellerPayoutItem>> GetSellerPayoutsAsync(long sellerId, PayoutStatus? status, CancellationToken ct)
    {
        var items = await _payouts.GetBySellerAsync(sellerId, status, ct);
        return items.Select(x => new SellerPayoutItem(x.Id, x.OrderId, x.Amount, x.Status, x.CreatedAt, x.PaidAt, x.BankTransferRef)).ToList();
    }

    public async Task<IReadOnlyList<AdminPayoutItem>> GetAdminPayoutsAsync(PayoutStatus? status, CancellationToken ct)
    {
        var items = await _payouts.GetAllAsync(status, ct);
        return items.Select(x => new AdminPayoutItem(
            x.Id,
            x.OrderId,
            x.SellerId,
            x.Amount,
            x.Status,
            x.CreatedAt,
            x.PaidAt,
            x.BankTransferRef,
            x.Order is null ? null : new AdminPayoutOrderSummary(x.Order.Id, x.Order.UserId, x.Order.SellerId, x.Order.Total, x.Order.Status, x.Order.CreatedAt),
            x.Seller is null ? null : new AdminPayoutSellerSummary(x.Seller.Id, x.Seller.Username, x.Seller.FullName, x.Seller.AvatarUrl, x.Seller.AvgSellerRating, x.Seller.TotalRatings)
        )).ToList();
    }

    public async Task<(bool Success, string? Error, MarkPayoutPaidResponse? Data)> MarkPaidAsync(long payoutId, MarkPayoutPaidRequest request, CancellationToken ct)
    {
        var payout = await _payouts.GetByIdAsync(payoutId, ct);
        if (payout is null) return (false, "Payout not found.", null);

        if (payout.Status == PayoutStatus.Paid)
            return (true, null, new MarkPayoutPaidResponse(payout.Id, payout.Status, payout.PaidAt ?? DateTimeOffset.UtcNow, payout.BankTransferRef));

        payout.Status = PayoutStatus.Paid;
        payout.PaidAt = DateTimeOffset.UtcNow;
        payout.BankTransferRef = string.IsNullOrWhiteSpace(request.BankTransferRef) ? payout.BankTransferRef : request.BankTransferRef.Trim();
        payout.UpdatedAt = DateTimeOffset.UtcNow;

        _notifications.Add(_notificationFactory.PayoutPaid(payout.SellerId, payout.Id, payout.Amount));

        await _uow.SaveChangesAsync(ct);

        return (true, null, new MarkPayoutPaidResponse(payout.Id, payout.Status, payout.PaidAt.Value, payout.BankTransferRef));
    }

    public async Task<(bool Success, string? Error, RejectPayoutResponse? Data)> RejectAsync(long payoutId, RejectPayoutRequest request, CancellationToken ct)
    {
        var payout = await _payouts.GetByIdAsync(payoutId, ct);
        if (payout is null) return (false, "Payout not found.", null);

        if (payout.Status == PayoutStatus.Failed)
            return (true, null, new RejectPayoutResponse(payout.Id, payout.Status, request.Reason));

        if (payout.Status == PayoutStatus.Paid)
            return (false, "Paid payout cannot be rejected.", null);

        payout.Status = PayoutStatus.Failed;
        payout.UpdatedAt = DateTimeOffset.UtcNow;
        await _uow.SaveChangesAsync(ct);

        return (true, null, new RejectPayoutResponse(payout.Id, payout.Status, request.Reason));
    }
}
