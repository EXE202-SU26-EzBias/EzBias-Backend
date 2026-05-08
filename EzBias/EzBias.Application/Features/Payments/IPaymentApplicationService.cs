using EzBias.Application.Features.Payments.Dtos;

namespace EzBias.Application.Features.Payments;

public interface IPaymentApplicationService
{
    Task<(bool Success, string? Error, CreatePaymentResponse? Data)> CreateAsync(long userId, CreatePaymentRequest request, CancellationToken ct);
    Task<(bool Success, string? Error, PaymentStatusResponse? Data)> GetStatusAsync(long userId, long paymentId, CancellationToken ct);
    Task<(bool Success, string? Error)> HandleWebhookAsync(PaymentWebhookRequest request, CancellationToken ct);
    Task<(bool Success, string? Error)> MarkPaidManualAsync(long userId, long paymentId, CancellationToken ct);
}
