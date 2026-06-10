using EzBias.Application.Features.Disputes.Dtos;
using EzBias.Application.Features.Notifications;
using EzBias.Application.Features.Orders;
using EzBias.Domain.Entities;
using EzBias.Domain.Enums;
using EzBias.Domain.Interfaces;

namespace EzBias.Application.Features.Disputes;

public class DisputeApplicationService : IDisputeApplicationService
{
    private readonly IDisputeRepository _disputes;
    private readonly IOrderRepository _orders;
    private readonly IPaymentRepository _payments;
    private readonly IRefundRepository _refunds;
    private readonly IPayoutRepository _payouts;
    private readonly IOrderApplicationService _orderService;
    private readonly INotificationRepository _notifications;
    private readonly INotificationFactory _notificationFactory;
    private readonly IUnitOfWork _uow;

    public DisputeApplicationService(
        IDisputeRepository disputes,
        IOrderRepository orders,
        IPaymentRepository payments,
        IRefundRepository refunds,
        IPayoutRepository payouts,
        IOrderApplicationService orderService,
        INotificationRepository notifications,
        INotificationFactory notificationFactory,
        IUnitOfWork uow)
    {
        _disputes = disputes;
        _orders = orders;
        _payments = payments;
        _refunds = refunds;
        _payouts = payouts;
        _orderService = orderService;
        _notifications = notifications;
        _notificationFactory = notificationFactory;
        _uow = uow;
    }

    public async Task<(bool Success, string? Error, DisputeResponse? Data)> CreateAsync(long buyerId, CreateDisputeRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Reason)) return (false, "Reason is required.", null);
        if (request.Items is null || request.Items.Count == 0) return (false, "At least one disputed item is required.", null);

        var order = await _orders.GetByIdWithItemsAsync(request.OrderId, ct);
        if (order is null) return (false, "Order not found.", null);
        if (order.UserId != buyerId) return (false, "Forbidden.", null);
        if (order.Status != OrderStatus.Delivered) return (false, "Refund request is only allowed within Delivered grace period.", null);
        if (!order.DeliveredAt.HasValue || order.DeliveredAt.Value.AddDays(3) < DateTimeOffset.UtcNow) return (false, "Refund window has expired.", null);

        var existing = await _disputes.GetOpenByOrderIdAsync(order.Id, ct);
        if (existing is not null) return (false, "An open dispute already exists for this order.", null);

        var orderItemMap = order.Items.ToDictionary(x => x.Id);
        var duplicate = request.Items.GroupBy(x => x.OrderItemId).FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null) return (false, "Duplicate order items are not allowed in dispute.", null);

        // A previously rejected dispute (ResolvedSeller) leaves a row behind; the order has a unique
        // dispute constraint, so reuse that row instead of inserting a second one.
        var prior = await _disputes.GetByOrderIdWithItemsAsync(order.Id, ct);

        var dispute = prior ?? new Dispute
        {
            OrderId = order.Id,
            InitiatorId = buyerId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dispute.Reason = request.Reason.Trim();
        dispute.Status = DisputeStatus.Open;
        dispute.InitiatorId = buyerId;
        dispute.AdminNote = null;
        dispute.ResolvedAt = null;

        if (prior is not null && prior.Items.Count > 0)
            _disputes.RemoveItems(prior.Items.ToList());

        var disputeItems = new List<DisputeItem>();
        foreach (var item in request.Items)
        {
            if (!orderItemMap.TryGetValue(item.OrderItemId, out var orderItem)) return (false, "Disputed item does not belong to order.", null);
            if (item.RequestedQty <= 0) return (false, "Requested quantity must be greater than zero.", null);
            if (item.RequestedQty > orderItem.Quantity) return (false, "Requested quantity exceeds ordered quantity.", null);

            disputeItems.Add(new DisputeItem
            {
                Dispute = dispute,
                OrderItemId = orderItem.Id,
                RequestedQty = item.RequestedQty,
                Note = item.Reason?.Trim(),
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        order.Status = OrderStatus.ReturnRequested;
        order.UpdatedAt = DateTimeOffset.UtcNow;

        if (prior is null)
            _disputes.Add(dispute);
        _disputes.AddItems(disputeItems);

        // Notify seller that a dispute was opened
        _notifications.Add(_notificationFactory.DisputeOpened(order.SellerId, dispute.Id, order.Id));

        await _uow.SaveChangesAsync(ct);

        dispute.Items = disputeItems;
        return (true, null, Map(dispute));
    }

    public async Task<(bool Success, string? Error, DisputeResponse? Data)> ApproveAsync(long adminId, long disputeId, ResolveDisputeRequest request, CancellationToken ct)
    {
        if (request.ApprovedItems is null || request.ApprovedItems.Count == 0) return (false, "At least one approved item is required.", null);

        var dispute = await _disputes.GetByIdAsync(disputeId, ct);
        if (dispute is null) return (false, "Dispute not found.", null);
        if (dispute.Status != DisputeStatus.Open && dispute.Status != DisputeStatus.UnderReview) return (false, "Dispute already resolved.", null);

        var order = await _orders.GetByIdWithItemsAsync(dispute.OrderId, ct);
        if (order is null) return (false, "Order not found.", null);

        var payout = await _payouts.GetByOrderIdAsync(order.Id, ct);
        if (payout is not null && payout.Status == PayoutStatus.Approved) return (false, "Payout already paid. Manual recovery required.", null);

        var payment = await _payments.GetByOrderIdAsync(order.Id, ct);
        if (payment is null) return (false, "Payment not found for order.", null);

        var disputeItemMap = dispute.Items.ToDictionary(x => x.OrderItemId);
        var orderItemMap = order.Items.ToDictionary(x => x.Id);
        var duplicate = request.ApprovedItems.GroupBy(x => x.OrderItemId).FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null) return (false, "Duplicate approved items are not allowed.", null);

        decimal refundAmount = 0m;
        foreach (var approved in request.ApprovedItems)
        {
            if (!disputeItemMap.TryGetValue(approved.OrderItemId, out var disputeItem)) return (false, "Cannot approve item that is not in dispute.", null);
            if (!orderItemMap.TryGetValue(approved.OrderItemId, out var orderItem)) return (false, "Disputed order item not found.", null);
            if (approved.ApprovedQty < 0) return (false, "Approved quantity cannot be negative.", null);
            if (approved.ApprovedQty > disputeItem.RequestedQty) return (false, "Approved quantity exceeds requested quantity.", null);

            disputeItem.ApprovedQty = approved.ApprovedQty;
            disputeItem.Note = approved.Note?.Trim() ?? disputeItem.Note;
            refundAmount += approved.ApprovedQty * orderItem.UnitPrice;
        }

        if (refundAmount <= 0m) return (false, "Total approved refund amount must be greater than zero.", null);
        if (refundAmount > order.Total) return (false, "Approved refund exceeds order total.", null);

        var processedTotal = await _refunds.GetProcessedTotalByPaymentIdAsync(payment.Id, ct);
        var refundable = payment.Amount - processedTotal;
        if (refundable < refundAmount) return (false, "Insufficient refundable amount.", null);

        var refund = new Refund
        {
            PaymentId = payment.Id,
            OrderId = order.Id,
            DisputeId = dispute.Id,
            Amount = refundAmount,
            Reason = $"Approved partial/full item refund by admin {adminId}.",
            Status = RefundStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _refunds.Add(refund);

        dispute.Status = DisputeStatus.ResolvedBuyer;
        dispute.AdminNote = request.AdminNote?.Trim();
        dispute.ResolvedAt = DateTimeOffset.UtcNow;

        // Notify both buyer (won) and seller (lost)
        _notifications.Add(_notificationFactory.DisputeResolved(dispute.InitiatorId, dispute.Id, resolvedForBuyer: true));
        _notifications.Add(_notificationFactory.DisputeResolved(order.SellerId, dispute.Id, resolvedForBuyer: false));

        await _uow.SaveChangesAsync(ct);
        return (true, null, Map(dispute));
    }

    public async Task<(bool Success, string? Error, DisputeResponse? Data)> RejectAsync(long adminId, long disputeId, RejectDisputeRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Reason)) return (false, "Reject reason is required.", null);

        var dispute = await _disputes.GetByIdAsync(disputeId, ct);
        if (dispute is null) return (false, "Dispute not found.", null);
        if (dispute.Status != DisputeStatus.Open && dispute.Status != DisputeStatus.UnderReview) return (false, "Dispute already resolved.", null);

        var order = await _orders.GetByIdAsync(dispute.OrderId, ct);
        if (order is null) return (false, "Order not found.", null);

        dispute.Status = DisputeStatus.ResolvedSeller;
        dispute.AdminNote = request.Reason.Trim();
        dispute.ResolvedAt = DateTimeOffset.UtcNow;

        order.Status = OrderStatus.Delivered;
        order.UpdatedAt = DateTimeOffset.UtcNow;

        // Notify buyer (lost) and seller (won)
        _notifications.Add(_notificationFactory.DisputeResolved(dispute.InitiatorId, dispute.Id, resolvedForBuyer: false));
        _notifications.Add(_notificationFactory.DisputeResolved(order.SellerId, dispute.Id, resolvedForBuyer: true));

        await _uow.SaveChangesAsync(ct);
        return (true, null, Map(dispute));
    }

    public async Task<(bool Success, string? Error, DisputeResponse? Data)> CompleteRefundPaymentAsync(long adminId, long disputeId, CompleteRefundPaymentRequest request, CancellationToken ct)
    {
        var dispute = await _disputes.GetByIdAsync(disputeId, ct);
        if (dispute is null) return (false, "Dispute not found.", null);

        var refund = await _refunds.GetLatestByDisputeIdAsync(disputeId, ct);
        if (refund is null) return (false, "Refund not found for dispute.", null);
        if (refund.Status != RefundStatus.Pending) return (false, "Refund already finalized.", null);

        var order = await _orders.GetByIdAsync(dispute.OrderId, ct);
        if (order is null) return (false, "Order not found.", null);

        var payment = await _payments.GetByIdAsync(refund.PaymentId, ct);
        if (payment is null) return (false, "Payment not found.", null);

        refund.Status = RefundStatus.Processed;
        refund.ProcessedAt = DateTimeOffset.UtcNow;
        refund.ProviderRef = $"MANUAL-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}";

        var now = DateTimeOffset.UtcNow;
        var fullRefund = refund.Amount >= order.Total;
        order.Status = fullRefund ? OrderStatus.Refunded : OrderStatus.Completed;
        order.UpdatedAt = now;

        if (!fullRefund)
        {
            order.CompletedAt = now;
            await _orderService.FinalizeOrderPayoutAsync(order, now, ct);
        }

        var processedTotal = await _refunds.GetProcessedTotalByPaymentIdAsync(payment.Id, ct);
        var totalAfterThisRefund = processedTotal + refund.Amount;
        if (Math.Abs(totalAfterThisRefund - payment.Amount) < 0.01m)
        {
            payment.Status = PaymentStatus.Refunded;
            payment.UpdatedAt = DateTimeOffset.UtcNow;
        }

        // Notify buyer that the refund has been paid out
        _notifications.Add(_notificationFactory.DisputeRefundCompleted(dispute.InitiatorId, dispute.Id, refund.Amount));

        await _uow.SaveChangesAsync(ct);
        return (true, null, Map(dispute));
    }

    public async Task<IReadOnlyList<DisputeListItemResponse>> GetListAsync(CancellationToken ct)
    {
        var disputes = await _disputes.GetAllWithOrderAndBuyerAsync(ct);
        return disputes.Select(MapListItem).ToList();
    }

    private static DisputeResponse Map(Dispute x) => new(
        x.Id,
        x.OrderId,
        x.InitiatorId,
        x.Status.ToString(),
        x.Reason,
        x.AdminNote,
        x.CreatedAt,
        x.ResolvedAt,
        MapItems(x.Items));

    private static IReadOnlyList<DisputeItemResponse> MapItems(IEnumerable<DisputeItem> items)
        => items
            .OrderBy(i => i.Id)
            .Select(i => new DisputeItemResponse(
                i.Id,
                i.OrderItemId,
                i.OrderItem?.ProductName ?? string.Empty,
                i.OrderItem?.Quantity ?? 0,
                i.OrderItem?.UnitPrice ?? 0m,
                i.RequestedQty,
                i.ApprovedQty,
                i.Note))
            .ToList();

    private static DisputeListItemResponse MapListItem(Dispute x)
    {
        var buyer = x.Order?.User;
        DisputeRefundPayoutInfo? payoutInfo = null;

        if (buyer is not null)
        {
            payoutInfo = new DisputeRefundPayoutInfo(
                buyer.Id,
                buyer.FullName,
                buyer.Email,
                buyer.Phone,
                buyer.BankName,
                buyer.BankAccountNumber,
                buyer.BankAccountName);
        }

        var refundProcessed = x.Refunds.Any(r => r.Status == RefundStatus.Processed);

        return new DisputeListItemResponse(
            x.Id,
            x.OrderId,
            x.InitiatorId,
            x.Status.ToString(),
            x.Reason,
            x.AdminNote,
            x.CreatedAt,
            x.ResolvedAt,
            refundProcessed,
            payoutInfo,
            MapItems(x.Items));
    }
}
