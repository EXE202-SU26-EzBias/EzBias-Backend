using EzBias.Domain.Enums;

namespace EzBias.Application.Features.Payouts.Dtos;

public record SellerPayoutItem(long PayoutId, long OrderId, decimal Amount, PayoutStatus Status, DateTimeOffset CreatedAt, DateTimeOffset? PaidAt, string? BankTransferRef);
public record MarkPayoutPaidRequest(string? BankTransferRef);
public record MarkPayoutPaidResponse(long PayoutId, PayoutStatus Status, DateTimeOffset PaidAt, string? BankTransferRef);
public record RequestPayoutResponse(long PayoutId, long OrderId, decimal Amount, PayoutStatus Status, DateTimeOffset CreatedAt);
public record AdminPayoutItem(long PayoutId, long OrderId, long SellerId, decimal Amount, PayoutStatus Status, DateTimeOffset CreatedAt, DateTimeOffset? PaidAt, string? BankTransferRef);
public record RejectPayoutRequest(string? Reason);
public record RejectPayoutResponse(long PayoutId, PayoutStatus Status, string? Reason);
