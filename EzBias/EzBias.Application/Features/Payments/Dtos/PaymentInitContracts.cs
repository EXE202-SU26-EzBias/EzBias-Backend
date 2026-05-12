namespace EzBias.Application.Features.Payments.Dtos;

public record CreatePaymentRequest(IReadOnlyList<long> OrderIds);
public record CreatePaymentResponse(long PaymentId, string Reference, decimal Amount, string Status);
public record PaymentOrderSummary(long OrderId, decimal Total, EzBias.Domain.Enums.OrderStatus Status, long UserId, long SellerId);
public record PaymentStatusResponse(long PaymentId, string Reference, decimal Amount, string Status, DateTimeOffset CreatedAt, DateTimeOffset? PaidAt, IReadOnlyList<long>? OrderIds = null, IReadOnlyList<PaymentOrderSummary>? Orders = null);
public record PaymentWebhookRequest(string Reference, string? ProviderTxnId, string? TransferContent, string? Payload);
public record SePayWebhookPayload(
    string? Gateway,
    string? TransactionDate,
    string? AccountNumber,
    string? SubAccount,
    string? Code,
    string? Content,
    string? TransferType,
    string? Description,
    decimal? TransferAmount,
    string? ReferenceCode,
    decimal? Accumulated,
    long? Id);
