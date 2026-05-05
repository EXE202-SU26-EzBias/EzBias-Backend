using EzBias.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EzBias.Infrastructure.Persistence.Configurations;

public sealed class PayoutConfiguration : IEntityTypeConfiguration<Payout>
{
    public void Configure(EntityTypeBuilder<Payout> builder)
    {
        builder.ToTable("payouts");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(x => x.OrderId).HasColumnName("order_id").IsRequired();
        builder.Property(x => x.SellerId).HasColumnName("seller_id").IsRequired();
        builder.Property(x => x.Amount).HasColumnName("amount").HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasColumnType("text").HasDefaultValue(Domain.Enums.PayoutStatus.Pending).IsRequired();
        builder.Property(x => x.BankTransferRef).HasColumnName("bank_transfer_ref").HasColumnType("text");
        builder.Property(x => x.PaidAt).HasColumnName("paid_at").HasColumnType("timestamptz");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");

        builder.HasOne(x => x.Order)
            .WithOne(x => x.Payout)
            .HasForeignKey<Payout>(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Seller)
            .WithMany(x => x.Payouts)
            .HasForeignKey(x => x.SellerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.OrderId).IsUnique();
        builder.HasIndex(x => x.SellerId);
        builder.HasIndex(x => x.Status).HasDatabaseName("idx_payouts_pending");
    }
}
