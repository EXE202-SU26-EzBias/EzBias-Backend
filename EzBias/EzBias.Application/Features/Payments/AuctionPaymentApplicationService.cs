using EzBias.Application.Common.Results;
using EzBias.Application.Features.Payments.Dtos;
using EzBias.Domain.Interfaces;

namespace EzBias.Application.Features.Payments;

public sealed class AuctionPaymentApplicationService : IAuctionPaymentApplicationService
{
    private readonly IOrderRepository _orders;
    private readonly IPaymentRepository _payments;
    private readonly IPaymentApplicationService _paymentService;

    public AuctionPaymentApplicationService(
        IOrderRepository orders,
        IPaymentRepository payments,
        IPaymentApplicationService paymentService)
    {
        _orders = orders;
        _payments = payments;
        _paymentService = paymentService;
    }

    public async Task<Result<PaymentStatusResponse>> PayAsync(
        long userId,
        long auctionId,
        CancellationToken ct)
    {
        var order = await _orders.GetByAuctionIdAsync(auctionId, ct);
        if (order is null)
            return Result<PaymentStatusResponse>.Fail(
                "Auction order not found.",
                ApplicationErrorCode.ResourceNotFound);
        if (order.UserId != userId)
            return Result<PaymentStatusResponse>.Fail(
                "Forbidden.",
                ApplicationErrorCode.Forbidden);

        var payment = await _payments.GetPendingByAuctionIdAsync(auctionId, ct);
        if (payment is null)
            return Result<PaymentStatusResponse>.Fail(
                "Pending payment not found.",
                ApplicationErrorCode.ResourceNotFound);

        var confirmation = await _paymentService.HandleWebhookAsync(
            new PaymentWebhookRequest(
                payment.Reference,
                payment.ProviderTxnId,
                payment.TransferContent,
                payment.Payload),
            "{}",
            null,
            null,
            ct);
        if (!confirmation.IsSuccess)
            return Result<PaymentStatusResponse>.Fail(
                confirmation.Error ?? "Payment confirmation failed.",
                ApplicationErrorCode.Validation);

        var status = await _paymentService.GetStatusAsync(userId, payment.Id, ct);
        if (!status.IsSuccess || status.Value is null)
            return Result<PaymentStatusResponse>.Fail(
                status.Error ?? "Payment status could not be loaded.",
                ApplicationErrorCode.Validation);

        return Result<PaymentStatusResponse>.Ok(status.Value);
    }
}
