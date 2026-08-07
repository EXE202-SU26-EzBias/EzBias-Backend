using EzBias.Application.Common.Results;
using EzBias.Application.Features.Deposits.Dtos;

namespace EzBias.Application.Features.Deposits;

public interface IDepositApplicationService
{
    Task<Result<InitiateDepositResponse>> InitiateDepositAsync(
        long userId, long auctionId, CancellationToken ct);

    Task<Result<DepositStatusResponse>> GetMyDepositStatusAsync(
        long userId, long auctionId, CancellationToken ct);

    Task<Result> ConfirmDepositAsync(long paymentId, CancellationToken ct);

    Task<Result> RefundNonWinnerDepositsAsync(
        long auctionId, long? winnerId, CancellationToken ct);

    Task<Result> ApplyWinnerDepositAsync(long auctionId, long winnerId, CancellationToken ct);

    Task<Result<decimal>> ComputeWinnerAmountDueAsync(
        long auctionId, long winnerId, decimal finalPrice, CancellationToken ct);

    Task<Result> ForfeitWinnerDepositAsync(long auctionId, long winnerId, CancellationToken ct);

    Task<Result> ReleaseDepositsOnCancelAsync(long auctionId, CancellationToken ct);

    Task<Result<IReadOnlyList<AdminDepositListItem>>> GetPendingRefundsAsync(
        CancellationToken ct);

    Task<Result<AdminDepositDetailResponse>> GetDepositDetailAsync(
        long depositId, CancellationToken ct);

    Task<Result> ProcessManualRefundAsync(
        long depositId, string reason, CancellationToken ct);
}
