using EzBias.Application.Features.Payouts.Dtos;
using EzBias.Domain.Enums;
using EzBias.Domain.Interfaces;

namespace EzBias.Application.Features.Payouts;

public class PayoutApplicationService : IPayoutApplicationService
{
    private readonly IPayoutRepository _payouts;
    private readonly IUnitOfWork _uow;

    public PayoutApplicationService(IPayoutRepository payouts, IUnitOfWork uow)
    {
        _payouts = payouts;
        _uow = uow;
    }

    public async Task<IReadOnlyList<SellerPayoutItem>> GetSellerPayoutsAsync(long sellerId, PayoutStatus? status, CancellationToken ct)
    {
        var items = await _payouts.GetBySellerAsync(sellerId, status, ct);
        return items.Select(x => new SellerPayoutItem(x.Id, x.OrderId, x.Amount, x.Status, x.CreatedAt, x.PaidAt, x.BankTransferRef)).ToList();
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
}
