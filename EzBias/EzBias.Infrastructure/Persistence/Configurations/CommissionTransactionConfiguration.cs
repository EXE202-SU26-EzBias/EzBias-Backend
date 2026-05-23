using EzBias.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EzBias.Infrastructure.Persistence.Configurations;

public sealed class CommissionTransactionConfiguration : IEntityTypeConfiguration<CommissionTransaction>
{
    public void Configure(EntityTypeBuilder<CommissionTransaction> builder)
    {
        builder.ToTable("commission_transactions");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(x => x.OrderId).HasColumnName("order_id").IsRequired();
        builder.Property(x => x.PaymentId).HasColumnName("payment_id").IsRequired();
        builder.Property(x => x.SellerId).HasColumnName("seller_id").IsRequired();
        builder.Property(x => x.GrossAmount).HasColumnName("gross_amount").HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(x => x.CommissionRatePercent).HasColumnName("commission_rate_percent").HasColumnType("numeric(5,2)").IsRequired();
        builder.Property(x => x.CommissionAmount).HasColumnName("commission_amount").HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(x => x.SellerNetAmount).HasColumnName("seller_net_amount").HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(x => x.Currency).HasColumnName("currency").HasColumnType("char(3)").HasDefaultValue("VND").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").HasDefaultValueSql("now()").IsRequired();

        builder.HasOne(x => x.Order)
            .WithOne(x => x.CommissionTransaction)
            .HasForeignKey<CommissionTransaction>(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Payment)
            .WithMany(x => x.CommissionTransactions)
            .HasForeignKey(x => x.PaymentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Seller)
            .WithMany(x => x.CommissionTransactionsAsSeller)
            .HasForeignKey(x => x.SellerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.OrderId).IsUnique();
        builder.HasIndex(x => x.PaymentId);
        builder.HasIndex(x => new { x.SellerId, x.CreatedAt }).HasDatabaseName("idx_commission_seller_created_at");
        builder.HasIndex(x => x.CreatedAt).HasDatabaseName("idx_commission_created_at");
    }
}
