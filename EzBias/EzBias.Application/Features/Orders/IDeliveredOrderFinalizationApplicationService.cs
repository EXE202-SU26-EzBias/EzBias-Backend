namespace EzBias.Application.Features.Orders;

public sealed record DeliveredOrderFinalizationRunResult(
    int FinalizedCount,
    IReadOnlyList<string> Errors);

public interface IDeliveredOrderFinalizationApplicationService
{
    Task<DeliveredOrderFinalizationRunResult> RunAsync(
        DateTimeOffset now,
        int graceDays,
        CancellationToken ct);
}
