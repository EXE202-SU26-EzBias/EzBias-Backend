using EzBias.Application.Common.Results;
using EzBias.Application.Features.Payments.Dtos;

namespace EzBias.Application.Features.Payments;

public interface IAuctionPaymentApplicationService
{
    Task<Result<PaymentStatusResponse>> PayAsync(
        long userId,
        long auctionId,
        CancellationToken ct);
}
