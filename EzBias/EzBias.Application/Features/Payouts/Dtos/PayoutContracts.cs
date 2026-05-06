using EzBias.Domain.Enums;

namespace EzBias.Application.Features.Payouts.Dtos;

public record SellerPayoutItem(long PayoutId, long OrderId, decimal Amount, PayoutStatus Status, DateTimeOffset CreatedAt, DateTimeOffset? PaidAt, string? BankTransferRef);
public record MarkPayoutPaidRequest(string? BankTransferRef);
public record MarkPayoutPaidResponse(long PayoutId, PayoutStatus Status, DateTimeOffset PaidAt, string? BankTransferRef);
