using EzBias.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EzBias.Infrastructure.Persistence.Configurations;

public sealed class BidConfiguration : IEntityTypeConfiguration<Bid>
{
    public void Configure(EntityTypeBuilder<Bid> builder)
    {
        builder.ToTable("bids");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(x => x.AuctionId).HasColumnName("auction_id").IsRequired();
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.Amount).HasColumnName("amount").HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(x => x.IsWinning).HasColumnName("is_winning").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.UsernameSnap).HasColumnName("username_snap").HasColumnType("text").HasDefaultValue(string.Empty).IsRequired();
        builder.Property(x => x.AvatarSnap).HasColumnName("avatar_snap").HasColumnType("text").HasDefaultValue(string.Empty).IsRequired();
        builder.Property(x => x.AvatarBgSnap).HasColumnName("avatar_bg_snap").HasColumnType("text").HasDefaultValue(string.Empty).IsRequired();
        builder.Property(x => x.PlacedAt).HasColumnName("placed_at").HasColumnType("timestamptz").HasDefaultValueSql("now()").IsRequired();

        builder.HasOne(x => x.Auction)
            .WithMany(x => x.Bids)
            .HasForeignKey(x => x.AuctionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.User)
            .WithMany(x => x.Bids)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.AuctionId);
        builder.HasIndex(x => x.AuctionId)
            .IsUnique()
            .HasDatabaseName("uq_bids_one_winning_per_auction")
            .HasFilter("is_winning = true");
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => new { x.AuctionId, x.Amount }).HasDatabaseName("idx_bids_auction_amount");
    }
}
