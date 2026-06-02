using EzBias.Application.Features.Deposits.Dtos;

namespace EzBias.Application.Features.Deposits;

public interface IDepositApplicationService
{
    // Req 2 — bidder-initiated
    Task<(bool Success, string? Error, InitiateDepositResponse? Data)> InitiateDepositAsync(
        long userId, long auctionId, CancellationToken ct);

    // Req 9 — status query
    Task<(bool Success, string? Error, DepositStatusResponse? Data)> GetMyDepositStatusAsync(
        long userId, long auctionId, CancellationToken ct);

    // Req 3 — called from Payment_Service on confirm of an AuctionDeposit payment
    Task<(bool Success, string? Error)> ConfirmDepositAsync(long paymentId, CancellationToken ct);

    // Req 5 — scheduler-facing: refund all non-winner Held deposits (winnerId null => no-winner close)
    Task<(bool Success, string? Error)> RefundNonWinnerDepositsAsync(
        long auctionId, long? winnerId, CancellationToken ct);

    // Req 6 — apply winner's held deposit (Held -> Applied) once remaining balance is settled/zero-due
    Task<(bool Success, string? Error)> ApplyWinnerDepositAsync(long auctionId, long winnerId, CancellationToken ct);

    // Req 6.2/6.4 — compute amount due for the winner's final payment
    Task<(bool Success, string? Error, decimal AmountDue)> ComputeWinnerAmountDueAsync(
        long auctionId, long winnerId, decimal finalPrice, CancellationToken ct);

    // Req 7 — scheduler-facing: forfeit winner's held deposit (Held -> Forfeited)
    Task<(bool Success, string? Error)> ForfeitWinnerDepositAsync(long auctionId, long winnerId, CancellationToken ct);

    // Req 8 — release all Held deposits on cancellation (Held -> Refunded)
    Task<(bool Success, string? Error)> ReleaseDepositsOnCancelAsync(long auctionId, CancellationToken ct);

    // Admin deposit management
    Task<(bool Success, string? Error, IReadOnlyList<AdminDepositListItem>? Data)> GetPendingRefundsAsync(
        CancellationToken ct);

    Task<(bool Success, string? Error, AdminDepositDetailResponse? Data)> GetDepositDetailAsync(
        long depositId, CancellationToken ct);

    Task<(bool Success, string? Error)> ProcessManualRefundAsync(
        long depositId, string reason, CancellationToken ct);
}
