using EzBias.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EzBias.Infrastructure.Persistence.Configurations;

public sealed class AuctionDepositConfiguration : IEntityTypeConfiguration<AuctionDeposit>
{
    public void Configure(EntityTypeBuilder<AuctionDeposit> builder)
    {
        builder.ToTable("auction_deposits");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(x => x.AuctionId).HasColumnName("auction_id").IsRequired();
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.Amount).HasColumnName("amount").HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(x => x.State).HasColumnName("state").HasConversion<string>().HasColumnType("text").HasDefaultValue(Domain.Enums.DepositState.PendingPayment).IsRequired();

        builder.Property(x => x.PaymentId).HasColumnName("payment_id");
        builder.Property(x => x.RefundId).HasColumnName("refund_id");

        builder.Property(x => x.HeldAt).HasColumnName("held_at").HasColumnType("timestamptz");
        builder.Property(x => x.AppliedAt).HasColumnName("applied_at").HasColumnType("timestamptz");
        builder.Property(x => x.ForfeitedAt).HasColumnName("forfeited_at").HasColumnType("timestamptz");
        builder.Property(x => x.RefundedAt).HasColumnName("refunded_at").HasColumnType("timestamptz");

        builder.Property(x => x.LastError).HasColumnName("last_error").HasColumnType("text");
        builder.Property(x => x.ForfeitRetryCount).HasColumnName("forfeit_retry_count").HasDefaultValue(0).IsRequired();

        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");

        builder.HasOne(x => x.Auction)
            .WithMany(x => x.Deposits)
            .HasForeignKey(x => x.AuctionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Payment)
            .WithMany(x => x.AuctionDeposits)
            .HasForeignKey(x => x.PaymentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Refund)
            .WithMany()
            .HasForeignKey(x => x.RefundId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => new { x.UserId, x.AuctionId })
            .HasDatabaseName("uq_active_deposit_per_user_auction")
            .IsUnique()
            .HasFilter("state IN ('PendingPayment','Held')");

        builder.HasIndex(x => new { x.AuctionId, x.State });
        builder.HasIndex(x => x.PaymentId);

        builder.UseXminAsConcurrencyToken();
    }
}
