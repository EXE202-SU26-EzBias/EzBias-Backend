using EzBias.Application.Features.Disputes.Dtos;
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
    private readonly IUnitOfWork _uow;

    public DisputeApplicationService(IDisputeRepository disputes, IOrderRepository orders, IPaymentRepository payments, IRefundRepository refunds, IPayoutRepository payouts, IUnitOfWork uow)
    {
        _disputes = disputes;
        _orders = orders;
        _payments = payments;
        _refunds = refunds;
        _payouts = payouts;
        _uow = uow;
    }

    public async Task<(bool Success, string? Error, DisputeResponse? Data)> CreateAsync(long buyerId, CreateDisputeRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Reason)) return (false, "Reason is required.", null);

        var order = await _orders.GetByIdAsync(request.OrderId, ct);
        if (order is null) return (false, "Order not found.", null);
        if (order.UserId != buyerId) return (false, "Forbidden.", null);
        if (order.Status != OrderStatus.Delivered) return (false, "Refund request is only allowed within Delivered grace period.", null);
        if (!order.DeliveredAt.HasValue || order.DeliveredAt.Value.AddDays(3) < DateTimeOffset.UtcNow) return (false, "Refund window has expired.", null);

        var existing = await _disputes.GetOpenByOrderIdAsync(order.Id, ct);
        if (existing is not null) return (false, "An open dispute already exists for this order.", null);

        var dispute = new Dispute
        {
            OrderId = order.Id,
            InitiatorId = buyerId,
            Reason = request.Reason.Trim(),
            Status = DisputeStatus.Open,
            CreatedAt = DateTimeOffset.UtcNow
        };

        order.Status = OrderStatus.ReturnRequested;
        order.UpdatedAt = DateTimeOffset.UtcNow;

        _disputes.Add(dispute);
        await _uow.SaveChangesAsync(ct);

        return (true, null, Map(dispute));
    }

    public async Task<(bool Success, string? Error, DisputeResponse? Data)> ApproveAsync(long adminId, long disputeId, ResolveDisputeRequest request, CancellationToken ct)
    {
        var dispute = await _disputes.GetByIdAsync(disputeId, ct);
        if (dispute is null) return (false, "Dispute not found.", null);
        if (dispute.Status != DisputeStatus.Open && dispute.Status != DisputeStatus.UnderReview) return (false, "Dispute already resolved.", null);

        var order = await _orders.GetByIdAsync(dispute.OrderId, ct);
        if (order is null) return (false, "Order not found.", null);

        var payout = await _payouts.GetByOrderIdAsync(order.Id, ct);
        if (payout is not null && payout.Status == PayoutStatus.Paid) return (false, "Payout already paid. Manual recovery required.", null);

        var payment = await _payments.GetByOrderIdAsync(order.Id, ct);
        if (payment is null) return (false, "Payment not found for order.", null);

        var processedTotal = await _refunds.GetProcessedTotalByPaymentIdAsync(payment.Id, ct);
        var refundable = payment.Amount - processedTotal;
        if (refundable < order.Total) return (false, "Insufficient refundable amount.", null);

        var refund = new Refund
        {
            PaymentId = payment.Id,
            OrderId = order.Id,
            DisputeId = dispute.Id,
            Amount = order.Total,
            Reason = $"Approved full refund by admin {adminId}.",
            Status = RefundStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _refunds.Add(refund);

        dispute.Status = DisputeStatus.ResolvedBuyer;
        dispute.AdminNote = request.AdminNote?.Trim();
        dispute.ResolvedAt = DateTimeOffset.UtcNow;

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
        dispute.AdminNote = $"Rejected by admin {adminId}: {request.Reason.Trim()}";
        dispute.ResolvedAt = DateTimeOffset.UtcNow;

        order.Status = OrderStatus.Delivered;
        order.UpdatedAt = DateTimeOffset.UtcNow;

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
        refund.ProviderRef = string.IsNullOrWhiteSpace(request.ProviderRef)
            ? $"MANUAL-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}"
            : request.ProviderRef.Trim();
        if (!string.IsNullOrWhiteSpace(request.Note))
            refund.Reason = $"{refund.Reason} | PaymentNote: {request.Note.Trim()}";

        order.Status = OrderStatus.Refunded;
        order.UpdatedAt = DateTimeOffset.UtcNow;

        var processedTotal = await _refunds.GetProcessedTotalByPaymentIdAsync(payment.Id, ct);
        var totalAfterThisRefund = processedTotal + refund.Amount;
        if (Math.Abs(totalAfterThisRefund - payment.Amount) < 0.01m)
        {
            payment.Status = PaymentStatus.Refunded;
            payment.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await _uow.SaveChangesAsync(ct);
        return (true, null, Map(dispute));
    }

    public async Task<IReadOnlyList<DisputeListItemResponse>> GetListAsync(CancellationToken ct)
    {
        var disputes = await _disputes.GetAllWithOrderAndBuyerAsync(ct);
        return disputes.Select(MapListItem).ToList();
    }

    private static DisputeResponse Map(Dispute x) => new(x.Id, x.OrderId, x.InitiatorId, x.Status.ToString(), x.Reason, x.AdminNote, x.CreatedAt, x.ResolvedAt);

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

        return new DisputeListItemResponse(
            x.Id,
            x.OrderId,
            x.InitiatorId,
            x.Status.ToString(),
            x.Reason,
            x.AdminNote,
            x.CreatedAt,
            x.ResolvedAt,
            payoutInfo);
    }
}
