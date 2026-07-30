namespace EzBias.Application.Features.Deposits.Dtos;

public record InitiateDepositResponse(
    long DepositId, long AuctionId, string State,
    string PaymentReference, string TransferContent, decimal AmountDue, string Currency);

public record DepositStatusResponse(
    long AuctionId, decimal RequiredDepositAmount,
    bool HasDeposit, long? DepositId, decimal? Amount, string? State, string? PaymentReference);

public record AdminDepositListItem(
    long DepositId,
    long AuctionId,
    string AuctionTitle,
    long UserId,
    string UserEmail,
    string UserFullName,
    decimal Amount,
    DateTimeOffset HeldAt,
    string? PaymentReference,
    string State);

public record AdminDepositDetailResponse(
    long DepositId,
    long AuctionId,
    string AuctionTitle,
    string AuctionStatus,
    long? WinnerId,
    long UserId,
    string UserEmail,
    string UserFullName,
    string? BankName,
    string? BankAccountNumber,
    string? BankAccountName,
    decimal Amount,
    string State,
    DateTimeOffset HeldAt,
    long? PaymentId,
    string? PaymentReference,
    DateTimeOffset CreatedAt);

public record ProcessManualRefundRequest(string Reason);
