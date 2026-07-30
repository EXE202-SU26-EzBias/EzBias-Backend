using EzBias.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EzBias.Infrastructure.Persistence.Configurations;

public sealed class AuctionConfiguration : IEntityTypeConfiguration<Auction>
{
    public void Configure(EntityTypeBuilder<Auction> builder)
    {
        builder.ToTable("auctions");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(x => x.ProductId).HasColumnName("product_id").IsRequired();
        builder.Property(x => x.SellerId).HasColumnName("seller_id").IsRequired();

        builder.Property(x => x.FloorPrice).HasColumnName("floor_price").HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(x => x.ReservePrice).HasColumnName("reserve_price").HasColumnType("numeric(18,2)");
        builder.Property(x => x.CurrentBid).HasColumnName("current_bid").HasColumnType("numeric(18,2)").HasDefaultValue(0m).IsRequired();
        builder.Property(x => x.RequiredDepositAmount).HasColumnName("required_deposit_amount").HasColumnType("numeric(18,2)").HasDefaultValue(0m).IsRequired();
        builder.Property(x => x.IsUrgent).HasColumnName("is_urgent").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.HasProofImage).HasColumnName("has_proof_image").HasDefaultValue(false).IsRequired();

        builder.Property(x => x.ExtensionSeconds).HasColumnName("extension_seconds").HasDefaultValue(300).IsRequired();
        builder.Property(x => x.TriggerBeforeEnd).HasColumnName("trigger_before_end").HasDefaultValue(60).IsRequired();
        builder.Property(x => x.ExtensionCount).HasColumnName("extension_count").HasDefaultValue(0).IsRequired();

        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasColumnType("text").HasDefaultValue(Domain.Enums.AuctionStatus.Draft).IsRequired();
        builder.Property(x => x.WinnerId).HasColumnName("winner_id");
        builder.Property(x => x.FinalPrice).HasColumnName("final_price").HasColumnType("numeric(18,2)");
        builder.Property(x => x.EndsAt).HasColumnName("ends_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.EndedAt).HasColumnName("ended_at").HasColumnType("timestamptz");
        builder.Property(x => x.WinnerPaymentDeadline).HasColumnName("winner_payment_deadline").HasColumnType("timestamptz");
        builder.Property(x => x.ReminderSent5Min).HasColumnName("reminder_sent_5min").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");
        builder.UseXminAsConcurrencyToken();

        builder.HasOne(x => x.Product)
            .WithMany(x => x.Auctions)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Seller)
            .WithMany(x => x.AuctionsAsSeller)
            .HasForeignKey(x => x.SellerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Winner)
            .WithMany(x => x.AuctionsAsWinner)
            .HasForeignKey(x => x.WinnerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.SellerId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.ProductId).HasDatabaseName("uq_one_live_auction_per_product");
        builder.HasIndex(x => x.WinnerId);
        builder.HasIndex(x => x.EndsAt).HasDatabaseName("idx_auctions_ends_at");
    }
}
