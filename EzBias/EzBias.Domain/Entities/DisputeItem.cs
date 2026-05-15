namespace EzBias.Domain.Entities;

public class DisputeItem
{
    public long Id { get; set; }
    public long DisputeId { get; set; }
    public long OrderItemId { get; set; }
    public int RequestedQty { get; set; }
    public int? ApprovedQty { get; set; }
    public string? Note { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Dispute Dispute { get; set; } = null!;
    public OrderItem OrderItem { get; set; } = null!;
}
