using EzBias.Application.Common.Results;
using EzBias.Application.Features.Deposits.Dtos;

namespace EzBias.Application.Features.Deposits;

public interface IDepositApplicationService
{
    // Req 2 — bidder-initiated
    Task<Result<InitiateDepositResponse>> InitiateDepositAsync(
        long userId, long auctionId, CancellationToken ct);

    // Req 9 — status query
    Task<Result<DepositStatusResponse>> GetMyDepositStatusAsync(
        long userId, long auctionId, CancellationToken ct);

    // Req 3 — called from Payment_Service on confirm of an AuctionDeposit payment
    Task<Result> ConfirmDepositAsync(long paymentId, CancellationToken ct);

    // Req 5 — scheduler-facing: refund all non-winner Held deposits (winnerId null => no-winner close)
    Task<Result> RefundNonWinnerDepositsAsync(
        long auctionId, long? winnerId, CancellationToken ct);

    // Req 6 — apply winner's held deposit (Held -> Applied) once remaining balance is settled/zero-due
    Task<Result> ApplyWinnerDepositAsync(long auctionId, long winnerId, CancellationToken ct);

    // Req 6.2/6.4 — compute amount due for the winner's final payment
    Task<Result<decimal>> ComputeWinnerAmountDueAsync(
        long auctionId, long winnerId, decimal finalPrice, CancellationToken ct);

    // Req 7 — scheduler-facing: forfeit winner's held deposit (Held -> Forfeited)
    Task<Result> ForfeitWinnerDepositAsync(long auctionId, long winnerId, CancellationToken ct);

    // Req 8 — release all Held deposits on cancellation (Held -> Refunded)
    Task<Result> ReleaseDepositsOnCancelAsync(long auctionId, CancellationToken ct);

    // Admin deposit management
    Task<Result<IReadOnlyList<AdminDepositListItem>>> GetPendingRefundsAsync(
        CancellationToken ct);

    Task<Result<AdminDepositDetailResponse>> GetDepositDetailAsync(
        long depositId, CancellationToken ct);

    Task<Result> ProcessManualRefundAsync(
        long depositId, string reason, CancellationToken ct);
}
