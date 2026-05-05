using EzBias.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EzBias.Infrastructure.Persistence.Configurations;

public sealed class RatingConfiguration : IEntityTypeConfiguration<Rating>
{
    public void Configure(EntityTypeBuilder<Rating> builder)
    {
        builder.ToTable("ratings");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(x => x.OrderId).HasColumnName("order_id").IsRequired();
        builder.Property(x => x.BuyerId).HasColumnName("buyer_id").IsRequired();
        builder.Property(x => x.SellerId).HasColumnName("seller_id").IsRequired();
        builder.Property(x => x.ProductRating).HasColumnName("product_rating").IsRequired();
        builder.Property(x => x.SellerRating).HasColumnName("seller_rating").IsRequired();
        builder.Property(x => x.Tags).HasColumnName("tags").HasColumnType("text[]").HasDefaultValueSql("'{}'::text[]").IsRequired();
        builder.Property(x => x.Comment).HasColumnName("comment").HasColumnType("text");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").HasDefaultValueSql("now()").IsRequired();

        builder.HasOne(x => x.Order)
            .WithOne(x => x.Rating)
            .HasForeignKey<Rating>(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Buyer)
            .WithMany(x => x.RatingsAsBuyer)
            .HasForeignKey(x => x.BuyerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Seller)
            .WithMany(x => x.RatingsAsSeller)
            .HasForeignKey(x => x.SellerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.OrderId).IsUnique();
        builder.HasIndex(x => x.SellerId);
        builder.HasIndex(x => x.BuyerId);

        builder.HasCheckConstraint("ck_ratings_product_rating", "product_rating >= 1 AND product_rating <= 5");
        builder.HasCheckConstraint("ck_ratings_seller_rating", "seller_rating >= 1 AND seller_rating <= 5");
    }
}
