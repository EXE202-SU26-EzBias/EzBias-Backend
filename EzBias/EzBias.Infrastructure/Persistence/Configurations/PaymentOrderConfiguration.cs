using EzBias.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EzBias.Infrastructure.Persistence.Configurations;

public sealed class PaymentOrderConfiguration : IEntityTypeConfiguration<PaymentOrder>
{
    public void Configure(EntityTypeBuilder<PaymentOrder> builder)
    {
        builder.ToTable("payment_orders");

        builder.HasKey(x => new { x.PaymentId, x.OrderId });

        builder.Property(x => x.PaymentId).HasColumnName("payment_id");
        builder.Property(x => x.OrderId).HasColumnName("order_id");

        builder.HasOne(x => x.Payment)
            .WithMany(x => x.PaymentOrders)
            .HasForeignKey(x => x.PaymentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Order)
            .WithMany(x => x.PaymentOrders)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.OrderId);
    }
}
