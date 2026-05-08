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
    public Payout? Payout { get; set; }
    public Rating? Rating { get; set; }
    public Dispute? Dispute { get; set; }
    public ICollection<Refund> Refunds { get; set; } = new List<Refund>();
}
