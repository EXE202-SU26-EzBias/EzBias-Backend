namespace EzBias.Application.Features.Payments.Dtos;

public record CreatePaymentRequest(IReadOnlyList<long> OrderIds);
public record CreatePaymentResponse(long PaymentId, string Reference, decimal Amount, string Status, string TransferContent);
public record PaymentStatusResponse(long PaymentId, string Reference, decimal Amount, string Status, DateTimeOffset CreatedAt, DateTimeOffset? PaidAt);
public record PaymentWebhookRequest(string Reference, string? ProviderTxnId, string? TransferContent, string? Payload);
