using EzBias.Application.Common.Results;
using EzBias.Application.Features.Payouts.Dtos;
using EzBias.Domain.Enums;

namespace EzBias.Application.Features.Payouts;

public interface IPayoutApplicationService
{
    Task<IReadOnlyList<SellerPayoutItem>> GetSellerPayoutsAsync(long sellerId, PayoutStatus? status, CancellationToken ct);
    Task<IReadOnlyList<AdminPayoutItem>> GetAdminPayoutsAsync(PayoutStatus? status, CancellationToken ct);
    Task<Result<MarkPayoutPaidResponse>> MarkPaidAsync(long payoutId, MarkPayoutPaidRequest request, CancellationToken ct);
    Task<Result<RejectPayoutResponse>> RejectAsync(long payoutId, RejectPayoutRequest request, CancellationToken ct);
}
