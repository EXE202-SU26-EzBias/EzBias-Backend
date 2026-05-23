namespace EzBias.Domain.Entities;

public class CommissionTransaction
{
    public long Id { get; set; }
    public long OrderId { get; set; }
    public long PaymentId { get; set; }
    public long SellerId { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal CommissionRatePercent { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal SellerNetAmount { get; set; }
    public string Currency { get; set; } = "VND";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Order Order { get; set; } = null!;
    public Payment Payment { get; set; } = null!;
    public User Seller { get; set; } = null!;
}
