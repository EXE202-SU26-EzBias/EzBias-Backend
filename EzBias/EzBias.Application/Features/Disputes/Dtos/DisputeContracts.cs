namespace EzBias.Application.Features.Disputes.Dtos;

public record CreateDisputeRequest(long OrderId, string Reason);
public record ResolveDisputeRequest(string? AdminNote);
public record RejectDisputeRequest(string Reason);
public record CompleteRefundPaymentRequest(string? ProviderRef, string? Note);
public record DisputeResponse(long Id, long OrderId, long InitiatorId, string Status, string Reason, string? AdminNote, DateTimeOffset CreatedAt, DateTimeOffset? ResolvedAt);
public record DisputeRefundPayoutInfo(long BuyerId, string BuyerFullName, string BuyerEmail, string BuyerPhone, string BankName, string BankAccountNumber, string BankAccountName);
public record DisputeListItemResponse(long Id, long OrderId, long InitiatorId, string Status, string Reason, string? AdminNote, DateTimeOffset CreatedAt, DateTimeOffset? ResolvedAt, DisputeRefundPayoutInfo? RefundPayoutInfo);
