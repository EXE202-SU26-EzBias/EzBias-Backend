namespace EzBias.Domain.Entities;

public class OrderItem
{
    public long Id { get; set; }
    public long OrderId { get; set; }
    public long? ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductImage { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }

    public Order Order { get; set; } = null!;
    public Product? Product { get; set; }
}
