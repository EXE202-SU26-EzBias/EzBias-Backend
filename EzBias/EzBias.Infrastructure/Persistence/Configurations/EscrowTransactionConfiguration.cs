using EzBias.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EzBias.Infrastructure.Persistence.Configurations;

public sealed class EscrowTransactionConfiguration : IEntityTypeConfiguration<EscrowTransaction>
{
    public void Configure(EntityTypeBuilder<EscrowTransaction> builder)
    {
        builder.ToTable("escrow_transactions");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(x => x.OrderId).HasColumnName("order_id").IsRequired();
        builder.Property(x => x.SellerId).HasColumnName("seller_id").IsRequired();
        builder.Property(x => x.Type).HasColumnName("type").HasConversion<string>().HasColumnType("text").IsRequired();
        builder.Property(x => x.Amount).HasColumnName("amount").HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(x => x.PaymentId).HasColumnName("payment_id");
        builder.Property(x => x.PayoutId).HasColumnName("payout_id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").HasDefaultValueSql("now()").IsRequired();

        builder.HasOne(x => x.Order)
            .WithMany(x => x.EscrowTransactions)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Seller)
            .WithMany(x => x.EscrowTransactionsAsSeller)
            .HasForeignKey(x => x.SellerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Payment)
            .WithMany(x => x.EscrowTransactions)
            .HasForeignKey(x => x.PaymentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Payout)
            .WithMany(x => x.EscrowTransactions)
            .HasForeignKey(x => x.PayoutId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.OrderId);
        builder.HasIndex(x => x.SellerId);
        builder.HasIndex(x => x.PaymentId);
    }
}
