using EzBias.Application.Common.Results;
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
    private readonly IUserRepository _users;
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
        IUserRepository users,
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
        _users = users;
        _uow = uow;
    }

    public async Task<Result<DisputeResponse>> CreateAsync(long buyerId, CreateDisputeRequest request, CancellationToken ct)
    {
        await using var transaction = await _uow.BeginTransactionAsync(ct);

        if (string.IsNullOrWhiteSpace(request.Reason)) return Result<DisputeResponse>.Fail("Reason is required.", ApplicationErrorCode.Validation);
        if (request.Items is null || request.Items.Count == 0) return Result<DisputeResponse>.Fail("At least one disputed item is required.", ApplicationErrorCode.Validation);

        var order = await _orders.GetByIdWithItemsAsync(request.OrderId, ct);
        if (order is null) return Result<DisputeResponse>.Fail("Order not found.", ApplicationErrorCode.ResourceNotFound);
        if (order.UserId != buyerId) return Result<DisputeResponse>.Fail("Forbidden.", ApplicationErrorCode.Forbidden);
        if (order.Status != OrderStatus.Delivered) return Result<DisputeResponse>.Fail("Refund request is only allowed within Delivered grace period.", ApplicationErrorCode.Validation);
        if (!order.DeliveredAt.HasValue || order.DeliveredAt.Value.AddDays(3) < DateTimeOffset.UtcNow) return Result<DisputeResponse>.Fail("Refund window has expired.", ApplicationErrorCode.Validation);

        var existing = await _disputes.GetOpenByOrderIdAsync(order.Id, ct);
        if (existing is not null) return Result<DisputeResponse>.Fail("An open dispute already exists for this order.", ApplicationErrorCode.Validation);

        var orderItemMap = order.Items.ToDictionary(x => x.Id);
        var duplicate = request.Items.GroupBy(x => x.OrderItemId).FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null) return Result<DisputeResponse>.Fail("Duplicate order items are not allowed in dispute.", ApplicationErrorCode.Validation);

        var prior = await _disputes.GetByOrderIdWithItemsAsync(order.Id, ct);
        var dispute = prior ?? new Dispute
        {
            OrderId = order.Id,
            InitiatorId = buyerId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dispute.Reason = request.Reason.Trim();
        if (dispute.Open(DateTimeOffset.UtcNow) == TransitionOutcome.Invalid)
            return Result<DisputeResponse>.Fail("Dispute cannot be reopened.", ApplicationErrorCode.Validation);
        dispute.InitiatorId = buyerId;
        dispute.AdminNote = null;
        dispute.ResolvedAt = null;

        if (prior is not null && prior.Items.Count > 0)
            _disputes.RemoveItems(prior.Items.ToList());

        var disputeItems = new List<DisputeItem>();
        foreach (var item in request.Items)
        {
            if (!orderItemMap.TryGetValue(item.OrderItemId, out var orderItem)) return Result<DisputeResponse>.Fail("Disputed item does not belong to order.", ApplicationErrorCode.Validation);
            if (item.RequestedQty <= 0) return Result<DisputeResponse>.Fail("Requested quantity must be greater than zero.", ApplicationErrorCode.Validation);
            if (item.RequestedQty > orderItem.Quantity) return Result<DisputeResponse>.Fail("Requested quantity exceeds ordered quantity.", ApplicationErrorCode.Validation);

            disputeItems.Add(new DisputeItem
            {
                Dispute = dispute,
                OrderItemId = orderItem.Id,
                RequestedQty = item.RequestedQty,
                Note = item.Reason?.Trim(),
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        if (order.MarkReturnRequested(DateTimeOffset.UtcNow) == TransitionOutcome.Invalid)
            return Result<DisputeResponse>.Fail("Refund request is only allowed within Delivered grace period.", ApplicationErrorCode.Validation);

        if (prior is null)
            _disputes.Add(dispute);
        _disputes.AddItems(disputeItems);

        await _uow.SaveChangesAsync(ct);

        var adminIds = await _users.GetUserIdsByRoleAsync(UserRole.Admin, ct);
        if (adminIds.Count > 0)
        {
            _notifications.AddRange(adminIds.Select(adminId =>
                _notificationFactory.DisputePendingReview(adminId, dispute.Id, order.Id)));
            await _uow.SaveChangesAsync(ct);
        }

        await transaction.CommitAsync(ct);
        dispute.Items = disputeItems;
        return Result<DisputeResponse>.Ok(Map(dispute));
    }

    public async Task<Result<DisputeResponse>> ApproveAsync(long adminId, long disputeId, ResolveDisputeRequest request, CancellationToken ct)
    {
        await using var transaction = await _uow.BeginTransactionAsync(ct);

        if (request.ApprovedItems is null || request.ApprovedItems.Count == 0) return Result<DisputeResponse>.Fail("At least one approved item is required.", ApplicationErrorCode.Validation);

        var dispute = await _disputes.GetByIdAsync(disputeId, ct);
        if (dispute is null) return Result<DisputeResponse>.Fail("Dispute not found.", ApplicationErrorCode.ResourceNotFound);
        if (dispute.Status != DisputeStatus.Open && dispute.Status != DisputeStatus.UnderReview) return Result<DisputeResponse>.Fail("Dispute already resolved.", ApplicationErrorCode.Validation);

        var order = await _orders.GetByIdWithItemsAsync(dispute.OrderId, ct);
        if (order is null) return Result<DisputeResponse>.Fail("Order not found.", ApplicationErrorCode.ResourceNotFound);

        var payout = await _payouts.GetByOrderIdAsync(order.Id, ct);
        if (payout is not null && payout.Status == PayoutStatus.Approved) return Result<DisputeResponse>.Fail("Payout already paid. Manual recovery required.", ApplicationErrorCode.Conflict);

        var payment = await _payments.GetByOrderIdAsync(order.Id, ct);
        if (payment is null) return Result<DisputeResponse>.Fail("Payment not found for order.", ApplicationErrorCode.ResourceNotFound);

        var disputeItemMap = dispute.Items.ToDictionary(x => x.OrderItemId);
        var orderItemMap = order.Items.ToDictionary(x => x.Id);
        var duplicate = request.ApprovedItems.GroupBy(x => x.OrderItemId).FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null) return Result<DisputeResponse>.Fail("Duplicate approved items are not allowed.", ApplicationErrorCode.Validation);

        decimal refundAmount = 0m;
        foreach (var approved in request.ApprovedItems)
        {
            if (!disputeItemMap.TryGetValue(approved.OrderItemId, out var disputeItem)) return Result<DisputeResponse>.Fail("Cannot approve item that is not in dispute.", ApplicationErrorCode.Validation);
            if (!orderItemMap.TryGetValue(approved.OrderItemId, out var orderItem)) return Result<DisputeResponse>.Fail("Disputed order item not found.", ApplicationErrorCode.ResourceNotFound);
            if (approved.ApprovedQty < 0) return Result<DisputeResponse>.Fail("Approved quantity cannot be negative.", ApplicationErrorCode.Validation);
            if (approved.ApprovedQty > disputeItem.RequestedQty) return Result<DisputeResponse>.Fail("Approved quantity exceeds requested quantity.", ApplicationErrorCode.Validation);

            disputeItem.ApprovedQty = approved.ApprovedQty;
            disputeItem.Note = approved.Note?.Trim() ?? disputeItem.Note;
            refundAmount += approved.ApprovedQty * orderItem.UnitPrice;
        }

        if (refundAmount <= 0m) return Result<DisputeResponse>.Fail("Total approved refund amount must be greater than zero.", ApplicationErrorCode.Validation);
        if (refundAmount > order.Total) return Result<DisputeResponse>.Fail("Approved refund exceeds order total.", ApplicationErrorCode.Validation);

        var processedTotal = await _refunds.GetProcessedTotalByPaymentIdAsync(payment.Id, ct);
        var refundable = EffectiveAmountPaid(order, payment) - processedTotal;
        if (refundable < refundAmount) return Result<DisputeResponse>.Fail("Insufficient refundable amount.", ApplicationErrorCode.Validation);

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

        if (dispute.ResolveForBuyer(DateTimeOffset.UtcNow) == TransitionOutcome.Invalid)
            return Result<DisputeResponse>.Fail("Dispute already resolved.", ApplicationErrorCode.Validation);
        dispute.AdminNote = request.AdminNote?.Trim();

        _notifications.Add(_notificationFactory.DisputeResolved(dispute.InitiatorId, dispute.Id, resolvedForBuyer: true));

        await _uow.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Result<DisputeResponse>.Ok(Map(dispute));
    }

    public async Task<Result<DisputeResponse>> RejectAsync(long adminId, long disputeId, RejectDisputeRequest request, CancellationToken ct)
    {
        await using var transaction = await _uow.BeginTransactionAsync(ct);

        if (string.IsNullOrWhiteSpace(request.Reason)) return Result<DisputeResponse>.Fail("Reject reason is required.", ApplicationErrorCode.Validation);

        var dispute = await _disputes.GetByIdAsync(disputeId, ct);
        if (dispute is null) return Result<DisputeResponse>.Fail("Dispute not found.", ApplicationErrorCode.ResourceNotFound);
        if (dispute.Status != DisputeStatus.Open && dispute.Status != DisputeStatus.UnderReview) return Result<DisputeResponse>.Fail("Dispute already resolved.", ApplicationErrorCode.Validation);

        var order = await _orders.GetByIdAsync(dispute.OrderId, ct);
        if (order is null) return Result<DisputeResponse>.Fail("Order not found.", ApplicationErrorCode.ResourceNotFound);

        if (dispute.ResolveForSeller(DateTimeOffset.UtcNow) == TransitionOutcome.Invalid)
            return Result<DisputeResponse>.Fail("Dispute already resolved.", ApplicationErrorCode.Validation);
        dispute.AdminNote = request.Reason.Trim();

        if (order.MarkDelivered(DateTimeOffset.UtcNow) == TransitionOutcome.Invalid)
            return Result<DisputeResponse>.Fail("Order cannot be marked delivered in current status.", ApplicationErrorCode.Validation);

        _notifications.Add(_notificationFactory.DisputeResolved(dispute.InitiatorId, dispute.Id, resolvedForBuyer: false));

        await _uow.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Result<DisputeResponse>.Ok(Map(dispute));
    }

    public async Task<Result<DisputeResponse>> CompleteRefundPaymentAsync(long adminId, long disputeId, CompleteRefundPaymentRequest request, CancellationToken ct)
    {
        await using var transaction = await _uow.BeginTransactionAsync(ct);

        var dispute = await _disputes.GetByIdAsync(disputeId, ct);
        if (dispute is null) return Result<DisputeResponse>.Fail("Dispute not found.", ApplicationErrorCode.ResourceNotFound);

        var refund = await _refunds.GetLatestByDisputeIdAsync(disputeId, ct);
        if (refund is null) return Result<DisputeResponse>.Fail("Refund not found for dispute.", ApplicationErrorCode.ResourceNotFound);
        if (refund.Status != RefundStatus.Pending) return Result<DisputeResponse>.Fail("Refund already finalized.", ApplicationErrorCode.Validation);

        var order = await _orders.GetByIdAsync(dispute.OrderId, ct);
        if (order is null) return Result<DisputeResponse>.Fail("Order not found.", ApplicationErrorCode.ResourceNotFound);

        var payment = await _payments.GetByIdAsync(refund.PaymentId, ct);
        if (payment is null) return Result<DisputeResponse>.Fail("Payment not found.", ApplicationErrorCode.ResourceNotFound);

        refund.Status = RefundStatus.Completed;
        refund.ProcessedAt = DateTimeOffset.UtcNow;
        refund.ProviderRef = $"REF-DSP-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{dispute.Id}";

        var now = DateTimeOffset.UtcNow;
        var fullRefund = refund.Amount >= order.Total;
        if (order.MarkRefunded(fullRefund, now) == TransitionOutcome.Invalid)
            return Result<DisputeResponse>.Fail("Order cannot be refunded in current status.", ApplicationErrorCode.Validation);

        if (!fullRefund)
        {
            await _orderService.FinalizeOrderPayoutAsync(order, now, ct);
        }

        var processedTotal = await _refunds.GetProcessedTotalByPaymentIdAsync(payment.Id, ct);
        var totalAfterThisRefund = processedTotal + refund.Amount;
        if (Math.Abs(totalAfterThisRefund - EffectiveAmountPaid(order, payment)) < 0.01m)
        {
            if (payment.MarkRefunded(now) == TransitionOutcome.Invalid)
                return Result<DisputeResponse>.Fail("Payment cannot be refunded in current status.", ApplicationErrorCode.Validation);
        }

        _notifications.Add(_notificationFactory.DisputeRefundCompleted(dispute.InitiatorId, dispute.Id, refund.Amount));

        await _uow.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Result<DisputeResponse>.Ok(Map(dispute));
    }

    public async Task<IReadOnlyList<DisputeListItemResponse>> GetListAsync(CancellationToken ct)
    {
        var disputes = await _disputes.GetAllWithOrderAndBuyerAsync(ct);
        return disputes.Select(MapListItem).ToList();
    }

    private static decimal EffectiveAmountPaid(Order order, Payment payment)
        => payment.Amount >= order.Total ? payment.Amount : order.Total;

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

        var refundProcessed = x.Refunds.Any(r => r.Status == RefundStatus.Completed);

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
