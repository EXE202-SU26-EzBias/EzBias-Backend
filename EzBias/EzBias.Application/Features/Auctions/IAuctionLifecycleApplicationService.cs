namespace EzBias.Application.Features.Auctions;

public sealed record AuctionLifecycleRunResult(
    int RemindersSent,
    int EndedNoWinner,
    int PendingPayment,
    int WinnerFailed,
    IReadOnlyList<string> Errors);

public interface IAuctionLifecycleApplicationService
{
    Task<AuctionLifecycleRunResult> RunAsync(
        DateTimeOffset now,
        CancellationToken ct);
}
