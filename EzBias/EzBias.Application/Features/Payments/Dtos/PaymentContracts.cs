namespace EzBias.Application.Features.Payments.Dtos;

public record ConfirmPaymentResponse(long PaymentId, string Status, IReadOnlyList<long> OrderIds, int EscrowHoldCount);
