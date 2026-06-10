namespace EzBias.Application.Features.Disputes.Dtos;

public record CreateDisputeItemRequest(long OrderItemId, int RequestedQty, string? Reason);
public record ApproveDisputeItemRequest(long OrderItemId, int ApprovedQty, string? Note);

public record CreateDisputeRequest(long OrderId, string Reason, IReadOnlyList<CreateDisputeItemRequest> Items);
public record ResolveDisputeRequest(string? AdminNote, IReadOnlyList<ApproveDisputeItemRequest> ApprovedItems);
public record RejectDisputeRequest(string Reason);
public record CompleteRefundPaymentRequest();
public record DisputeItemResponse(long Id, long OrderItemId, string ProductName, int OrderedQty, decimal UnitPrice, int RequestedQty, int? ApprovedQty, string? Note);
public record DisputeResponse(long Id, long OrderId, long InitiatorId, string Status, string Reason, string? AdminNote, DateTimeOffset CreatedAt, DateTimeOffset? ResolvedAt, IReadOnlyList<DisputeItemResponse> Items);
public record DisputeRefundPayoutInfo(long BuyerId, string BuyerFullName, string BuyerEmail, string BuyerPhone, string BankName, string BankAccountNumber, string BankAccountName);
public record DisputeListItemResponse(long Id, long OrderId, long InitiatorId, string Status, string Reason, string? AdminNote, DateTimeOffset CreatedAt, DateTimeOffset? ResolvedAt, bool RefundProcessed, DisputeRefundPayoutInfo? RefundPayoutInfo, IReadOnlyList<DisputeItemResponse> Items);
