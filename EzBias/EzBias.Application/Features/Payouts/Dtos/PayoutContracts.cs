using EzBias.Domain.Enums;

namespace EzBias.Application.Features.Payouts.Dtos;

public record SellerPayoutItem(long PayoutId, long OrderId, decimal Amount, PayoutStatus Status, DateTimeOffset CreatedAt, DateTimeOffset? PaidAt, string? BankTransferRef);
public record MarkPayoutPaidRequest(string? BankTransferRef);
public record MarkPayoutPaidResponse(long PayoutId, PayoutStatus Status, DateTimeOffset PaidAt, string? BankTransferRef);
public record AdminPayoutItem(long PayoutId, long OrderId, long SellerId, decimal Amount, PayoutStatus Status, DateTimeOffset CreatedAt, DateTimeOffset? PaidAt, string? BankTransferRef, AdminPayoutOrderSummary? Order = null, AdminPayoutSellerSummary? Seller = null);
public record AdminPayoutOrderSummary(long Id, long UserId, long SellerId, decimal Total, EzBias.Domain.Enums.OrderStatus Status, DateTimeOffset CreatedAt);
public record AdminPayoutSellerSummary(long Id, string Username, string FullName, string AvatarUrl, decimal AvgSellerRating, int TotalRatings, string? BankName, string? BankAccountNumber, string? BankAccountName);
public record RejectPayoutRequest(string? Reason);
public record RejectPayoutResponse(long PayoutId, PayoutStatus Status, string? Reason);
