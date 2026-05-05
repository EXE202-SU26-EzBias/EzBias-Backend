using EzBias.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EzBias.Infrastructure.Persistence.Configurations;

public sealed class SellerFollowConfiguration : IEntityTypeConfiguration<SellerFollow>
{
    public void Configure(EntityTypeBuilder<SellerFollow> builder)
    {
        builder.ToTable("seller_follows");

        builder.HasKey(x => new { x.FollowerId, x.SellerId });

        builder.Property(x => x.FollowerId).HasColumnName("follower_id");
        builder.Property(x => x.SellerId).HasColumnName("seller_id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").HasDefaultValueSql("now()").IsRequired();

        builder.HasOne(x => x.Follower)
            .WithMany(x => x.FollowingSellers)
            .HasForeignKey(x => x.FollowerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Seller)
            .WithMany(x => x.Followers)
            .HasForeignKey(x => x.SellerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.SellerId);
    }
}
