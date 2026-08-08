using EzBias.Application.Common.Results;
using EzBias.Application.Features.Payments.Dtos;

namespace EzBias.Application.Features.Payments;

public interface IPaymentApplicationService
{
    Task<Result<CreatePaymentResponse>> CreateAsync(long userId, CreatePaymentRequest request, CancellationToken ct);
    Task<Result<PaymentStatusResponse>> GetStatusAsync(long userId, long paymentId, CancellationToken ct);
    Task<Result> ConfirmAuctionDepositAsync(long userId, long auctionId, CancellationToken ct);
    Task<Result> ConfirmManualAsync(long adminId, long paymentId, CancellationToken ct);
    Task<Result> HandleWebhookAsync(PaymentWebhookRequest request, string rawBody, string? signature, string? timestamp, CancellationToken ct);
    Task<Result> HandleSePayWebhookAsync(SePayWebhookPayload payload, string rawBody, string? signature, string? timestamp, CancellationToken ct);
}
