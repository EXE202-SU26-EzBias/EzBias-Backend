using EzBias.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EzBias.Infrastructure.Persistence.Configurations;

public sealed class ProductReviewMediaConfiguration : IEntityTypeConfiguration<ProductReviewMedia>
{
    public void Configure(EntityTypeBuilder<ProductReviewMedia> builder)
    {
        builder.ToTable("product_review_media");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(x => x.ProductReviewId).HasColumnName("product_review_id").IsRequired();
        builder.Property(x => x.MediaType).HasColumnName("media_type").HasConversion<short>().IsRequired();
        builder.Property(x => x.Url).HasColumnName("url").HasColumnType("text").IsRequired();
        builder.Property(x => x.ThumbnailUrl).HasColumnName("thumbnail_url").HasColumnType("text");
        builder.Property(x => x.CloudinaryPublicId).HasColumnName("cloudinary_public_id").HasColumnType("text").IsRequired();
        builder.Property(x => x.SortOrder).HasColumnName("sort_order").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").HasDefaultValueSql("now()").IsRequired();

        builder.HasOne(x => x.ProductReview)
            .WithMany(x => x.Media)
            .HasForeignKey(x => x.ProductReviewId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.ProductReviewId, x.SortOrder });
    }
}
