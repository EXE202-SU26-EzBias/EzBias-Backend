using EzBias.Application.Common.Results;
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
            x.Seller is null ? null : new AdminPayoutSellerSummary(x.Seller.Id, x.Seller.Username, x.Seller.FullName, x.Seller.AvatarUrl, x.Seller.AvgSellerRating, x.Seller.TotalRatings, x.Seller.BankName, x.Seller.BankAccountNumber, x.Seller.BankAccountName)
        )).ToList();
    }

    public async Task<Result<MarkPayoutPaidResponse>> MarkPaidAsync(long payoutId, MarkPayoutPaidRequest request, CancellationToken ct)
    {
        var payout = await _payouts.GetByIdAsync(payoutId, ct);
        if (payout is null) return (false, "Payout not found.", null);

        if (payout.Status == PayoutStatus.Approved)
            return (true, null, new MarkPayoutPaidResponse(payout.Id, payout.Status, payout.PaidAt ?? DateTimeOffset.UtcNow, payout.BankTransferRef));

        var now = DateTimeOffset.UtcNow;
        if (payout.Approve(now) == TransitionOutcome.Invalid)
            return (false, "Payout cannot be marked paid in current status.", null);
        payout.BankTransferRef = !string.IsNullOrWhiteSpace(request.BankTransferRef)
            ? request.BankTransferRef.Trim()
            : $"PO-{now:yyyyMMddHHmmss}-{payout.Id}";

        _notifications.Add(_notificationFactory.PayoutPaid(payout.SellerId, payout.Id, payout.Amount));

        await _uow.SaveChangesAsync(ct);

        return (true, null, new MarkPayoutPaidResponse(payout.Id, payout.Status, payout.PaidAt ?? now, payout.BankTransferRef));
    }

    public async Task<Result<RejectPayoutResponse>> RejectAsync(long payoutId, RejectPayoutRequest request, CancellationToken ct)
    {
        var payout = await _payouts.GetByIdAsync(payoutId, ct);
        if (payout is null) return (false, "Payout not found.", null);

        if (payout.Status == PayoutStatus.Rejected)
            return (true, null, new RejectPayoutResponse(payout.Id, payout.Status, request.Reason));

        if (payout.Status == PayoutStatus.Approved)
            return (false, "Approved payout cannot be rejected.", null);

        if (payout.Reject(DateTimeOffset.UtcNow) == TransitionOutcome.Invalid)
            return (false, "Approved payout cannot be rejected.", null);
        await _uow.SaveChangesAsync(ct);

        return (true, null, new RejectPayoutResponse(payout.Id, payout.Status, request.Reason));
    }
}
