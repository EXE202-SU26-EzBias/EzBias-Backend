using EzBias.Application.Features.Payments.Dtos;

namespace EzBias.Application.Features.Payments;

public interface IPaymentApplicationService
{
    Task<(bool Success, string? Error, ConfirmPaymentResponse? Data)> ConfirmAsync(long userId, long paymentId, CancellationToken ct);
}
