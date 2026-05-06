using EzBias.Application.Features.Payouts.Dtos;
using EzBias.Domain.Enums;

namespace EzBias.Application.Features.Payouts;

public interface IPayoutApplicationService
{
    Task<IReadOnlyList<SellerPayoutItem>> GetSellerPayoutsAsync(long sellerId, PayoutStatus? status, CancellationToken ct);
    Task<(bool Success, string? Error, MarkPayoutPaidResponse? Data)> MarkPaidAsync(long payoutId, MarkPayoutPaidRequest request, CancellationToken ct);
}
