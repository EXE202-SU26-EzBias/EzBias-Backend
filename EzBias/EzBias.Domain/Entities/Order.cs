using EzBias.Domain.Enums;

namespace EzBias.Domain.Entities;

public class Order
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public long SellerId { get; set; }
    public OrderSource Source { get; set; } = OrderSource.Cart;
    public long? AuctionId { get; set; }

    public decimal Total { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public string AddressSnap { get; set; } = "{}"; // jsonb
    public string? Carrier { get; set; }
    public string? TrackingNumber { get; set; }
    public DateTimeOffset? ShippedAt { get; set; }
    public DateTimeOffset? DeliveredAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    public User User { get; set; } = null!;
    public User Seller { get; set; } = null!;
    public Auction? Auction { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public ICollection<PaymentOrder> PaymentOrders { get; set; } = new List<PaymentOrder>();
    public ICollection<EscrowTransaction> EscrowTransactions { get; set; } = new List<EscrowTransaction>();
    public CommissionTransaction? CommissionTransaction { get; set; }
    public Payout? Payout { get; set; }
    public Dispute? Dispute { get; set; }
    public ICollection<Refund> Refunds { get; set; } = new List<Refund>();

    public TransitionOutcome MarkPaid(DateTimeOffset now)
    {
        if (Status == OrderStatus.Paid)
            return TransitionOutcome.NoOp;
        if (Status is not (OrderStatus.Pending or OrderStatus.Processing))
            return TransitionOutcome.Invalid;
        Status = OrderStatus.Paid;
        UpdatedAt = now;
        return TransitionOutcome.Applied;
    }

    public TransitionOutcome MarkShipped(string? carrier, string trackingNumber, DateTimeOffset now)
    {
        if (Status == OrderStatus.Shipped)
            return TransitionOutcome.NoOp;
        if (Status is not (OrderStatus.Paid or OrderStatus.Processing))
            return TransitionOutcome.Invalid;
        Carrier = carrier;
        TrackingNumber = trackingNumber;
        ShippedAt = now;
        Status = OrderStatus.Shipped;
        UpdatedAt = now;
        return TransitionOutcome.Applied;
    }

    public TransitionOutcome MarkDelivered(DateTimeOffset now)
    {
        if (Status == OrderStatus.Delivered)
            return TransitionOutcome.NoOp;
        if (Status is not (OrderStatus.Shipped or OrderStatus.ReturnRequested))
            return TransitionOutcome.Invalid;
        DeliveredAt = now;
        Status = OrderStatus.Delivered;
        UpdatedAt = now;
        return TransitionOutcome.Applied;
    }

    public TransitionOutcome MarkCompleted(DateTimeOffset now)
    {
        if (Status == OrderStatus.Completed)
            return TransitionOutcome.NoOp;
        if (Status != OrderStatus.Delivered)
            return TransitionOutcome.Invalid;
        CompletedAt = now;
        Status = OrderStatus.Completed;
        UpdatedAt = now;
        return TransitionOutcome.Applied;
    }

    public TransitionOutcome MarkCanceled(DateTimeOffset now)
    {
        if (Status == OrderStatus.Canceled)
            return TransitionOutcome.NoOp;
        if (Status is not (OrderStatus.Pending or OrderStatus.Processing))
            return TransitionOutcome.Invalid;
        Status = OrderStatus.Canceled;
        UpdatedAt = now;
        return TransitionOutcome.Applied;
    }

    public TransitionOutcome MarkReturnRequested(DateTimeOffset now)
    {
        if (Status == OrderStatus.ReturnRequested)
            return TransitionOutcome.NoOp;
        if (Status != OrderStatus.Delivered)
            return TransitionOutcome.Invalid;
        Status = OrderStatus.ReturnRequested;
        UpdatedAt = now;
        return TransitionOutcome.Applied;
    }

    public TransitionOutcome MarkRefunded(bool fullRefund, DateTimeOffset now)
    {
        var target = fullRefund ? OrderStatus.Refunded : OrderStatus.Completed;
        if (Status == target)
            return TransitionOutcome.NoOp;
        if (Status is not (OrderStatus.Delivered or OrderStatus.ReturnRequested or OrderStatus.Paid or OrderStatus.Completed))
            return TransitionOutcome.Invalid;
        Status = target;
        if (!fullRefund)
            CompletedAt = now;
        UpdatedAt = now;
        return TransitionOutcome.Applied;
    }
}
