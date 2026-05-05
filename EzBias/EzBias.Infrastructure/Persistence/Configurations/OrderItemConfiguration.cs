using EzBias.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EzBias.Infrastructure.Persistence.Configurations;

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("order_items");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(x => x.OrderId).HasColumnName("order_id").IsRequired();
        builder.Property(x => x.ProductId).HasColumnName("product_id");
        builder.Property(x => x.ProductName).HasColumnName("product_name").HasColumnType("text").HasDefaultValue(string.Empty).IsRequired();
        builder.Property(x => x.ProductImage).HasColumnName("product_image").HasColumnType("text").HasDefaultValue(string.Empty).IsRequired();
        builder.Property(x => x.Quantity).HasColumnName("quantity").IsRequired();
        builder.Property(x => x.UnitPrice).HasColumnName("unit_price").HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(x => x.Subtotal).HasColumnName("subtotal").HasColumnType("numeric(18,2)").HasComputedColumnSql("quantity * unit_price", stored: true);

        builder.HasOne(x => x.Order)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.OrderId);
        builder.HasIndex(x => x.ProductId);
    }
}
