using EzBias.Application.Features.Payouts.Dtos;
using EzBias.Domain.Enums;
using EzBias.Domain.Interfaces;

namespace EzBias.Application.Features.Payouts;

public class PayoutApplicationService : IPayoutApplicationService
{
    private readonly IPayoutRepository _payouts;
    private readonly IOrderRepository _orders;
    private readonly IUnitOfWork _uow;

    public PayoutApplicationService(IPayoutRepository payouts, IOrderRepository orders, IUnitOfWork uow)
    {
        _payouts = payouts;
        _orders = orders;
        _uow = uow;
    }

    public async Task<IReadOnlyList<SellerPayoutItem>> GetSellerPayoutsAsync(long sellerId, PayoutStatus? status, CancellationToken ct)
    {
        var items = await _payouts.GetBySellerAsync(sellerId, status, ct);
        return items.Select(x => new SellerPayoutItem(x.Id, x.OrderId, x.Amount, x.Status, x.CreatedAt, x.PaidAt, x.BankTransferRef)).ToList();
    }

    public async Task<(bool Success, string? Error, RequestPayoutResponse? Data)> RequestAsync(long sellerId, long orderId, CancellationToken ct)
    {
        var order = await _orders.GetByIdAsync(orderId, ct);
        if (order is null) return (false, "Order not found.", null);
        if (order.SellerId != sellerId) return (false, "Forbidden.", null);
        if (order.Status != OrderStatus.Completed) return (false, "Order is not eligible for payout request.", null);

        var existing = await _payouts.GetByOrderIdAsync(orderId, ct);
        if (existing is not null)
            return (true, null, new RequestPayoutResponse(existing.Id, existing.OrderId, existing.Amount, existing.Status, existing.CreatedAt));

        var payout = new Domain.Entities.Payout
        {
            OrderId = order.Id,
            SellerId = order.SellerId,
            Amount = order.Total,
            Status = PayoutStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _payouts.Add(payout);
        await _uow.SaveChangesAsync(ct);

        return (true, null, new RequestPayoutResponse(payout.Id, payout.OrderId, payout.Amount, payout.Status, payout.CreatedAt));
    }

    public async Task<IReadOnlyList<AdminPayoutItem>> GetAdminPayoutsAsync(PayoutStatus? status, CancellationToken ct)
    {
        var items = await _payouts.GetAllAsync(status, ct);
        return items.Select(x => new AdminPayoutItem(x.Id, x.OrderId, x.SellerId, x.Amount, x.Status, x.CreatedAt, x.PaidAt, x.BankTransferRef)).ToList();
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
