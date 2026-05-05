using EzBias.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EzBias.Infrastructure.Persistence.Configurations;

public sealed class RefundConfiguration : IEntityTypeConfiguration<Refund>
{
    public void Configure(EntityTypeBuilder<Refund> builder)
    {
        builder.ToTable("refunds");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(x => x.PaymentId).HasColumnName("payment_id").IsRequired();
        builder.Property(x => x.OrderId).HasColumnName("order_id");
        builder.Property(x => x.DisputeId).HasColumnName("dispute_id");
        builder.Property(x => x.Amount).HasColumnName("amount").HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(x => x.Reason).HasColumnName("reason").HasColumnType("text").HasDefaultValue(string.Empty).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasColumnType("text").HasDefaultValue(Domain.Enums.RefundStatus.Pending).IsRequired();
        builder.Property(x => x.ProviderRef).HasColumnName("provider_ref").HasColumnType("text");
        builder.Property(x => x.ProcessedAt).HasColumnName("processed_at").HasColumnType("timestamptz");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").HasDefaultValueSql("now()").IsRequired();

        builder.HasOne(x => x.Payment)
            .WithMany(x => x.Refunds)
            .HasForeignKey(x => x.PaymentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Order)
            .WithMany(x => x.Refunds)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Dispute)
            .WithMany(x => x.Refunds)
            .HasForeignKey(x => x.DisputeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.PaymentId);
        builder.HasIndex(x => x.OrderId);
        builder.HasIndex(x => x.DisputeId);
        builder.HasIndex(x => x.Status);
    }
}
