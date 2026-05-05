namespace EzBias.Domain.Entities;

public class PaymentOrder
{
    public long PaymentId { get; set; }
    public long OrderId { get; set; }

    public Payment Payment { get; set; } = null!;
    public Order Order { get; set; } = null!;
}
