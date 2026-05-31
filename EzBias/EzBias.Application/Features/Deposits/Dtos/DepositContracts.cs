namespace EzBias.Application.Features.Deposits.Dtos;

public record InitiateDepositResponse(
    long DepositId, long AuctionId, string State,
    string PaymentReference, string TransferContent, decimal AmountDue, string Currency);

public record DepositStatusResponse(
    long AuctionId, decimal RequiredDepositAmount,
    bool HasDeposit, long? DepositId, decimal? Amount, string? State, string? PaymentReference);
