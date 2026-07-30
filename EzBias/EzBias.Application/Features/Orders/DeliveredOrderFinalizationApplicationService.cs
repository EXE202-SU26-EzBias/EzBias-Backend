using EzBias.Domain.Enums;
using EzBias.Domain.Interfaces;

namespace EzBias.Application.Features.Orders;

public sealed class DeliveredOrderFinalizationApplicationService
    : IDeliveredOrderFinalizationApplicationService
{
    private readonly IOrderRepository _orders;
    private readonly IOrderApplicationService _orderService;
    private readonly IUnitOfWork _uow;

    public DeliveredOrderFinalizationApplicationService(
        IOrderRepository orders,
        IOrderApplicationService orderService,
        IUnitOfWork uow)
    {
        _orders = orders;
        _orderService = orderService;
        _uow = uow;
    }

    public async Task<DeliveredOrderFinalizationRunResult> RunAsync(
        DateTimeOffset now,
        int graceDays,
        CancellationToken ct)
    {
        var deliveredBefore = now.AddDays(-Math.Max(0, graceDays));
        var candidateIds = (await _orders
                .GetDeliveredOverdueWithoutOpenDisputeOrPendingRefundAsync(
                    deliveredBefore,
                    ct))
            .Select(x => x.Id)
            .Distinct()
            .ToList();
        _uow.ClearTrackedChanges();

        var finalizedCount = 0;
        var errors = new List<string>();

        foreach (var orderId in candidateIds)
        {
            try
            {
                if (await FinalizeOneAsync(orderId, deliveredBefore, now, ct))
                    finalizedCount++;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                errors.Add($"Order {orderId}: {ex.Message}");
            }
            finally
            {
                _uow.ClearTrackedChanges();
            }
        }

        return new DeliveredOrderFinalizationRunResult(finalizedCount, errors);
    }

    private async Task<bool> FinalizeOneAsync(
        long orderId,
        DateTimeOffset deliveredBefore,
        DateTimeOffset now,
        CancellationToken ct)
    {
        await using var transaction = await _uow.BeginTransactionAsync(ct);
        var order = await _orders.GetByIdForUpdateAsync(orderId, ct);
        if (order is null
            || order.Status != OrderStatus.Delivered
            || !order.DeliveredAt.HasValue
            || order.DeliveredAt.Value > deliveredBefore
            || order.Dispute?.Status is DisputeStatus.Open or DisputeStatus.UnderReview
            || order.Refunds.Any(x => x.Status == RefundStatus.Pending)
            || order.MarkCompleted(now) == TransitionOutcome.Invalid)
            return false;

        await _orderService.FinalizeOrderPayoutAsync(order, now, ct);
        await _uow.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return true;
    }
}
