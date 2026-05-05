using EzBias.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EzBias.Infrastructure.Persistence.Configurations;

public sealed class PhotocardDetailConfiguration : IEntityTypeConfiguration<PhotocardDetail>
{
    public void Configure(EntityTypeBuilder<PhotocardDetail> builder)
    {
        builder.ToTable("photocard_details");

        builder.HasKey(x => x.ProductId);
        builder.Property(x => x.ProductId).HasColumnName("product_id");

        builder.Property(x => x.MemberName).HasColumnName("member_name").HasColumnType("text").HasDefaultValue(string.Empty).IsRequired();
        builder.Property(x => x.AlbumSeries).HasColumnName("album_series").HasColumnType("text").HasDefaultValue(string.Empty).IsRequired();
        builder.Property(x => x.Version).HasColumnName("version").HasColumnType("text").HasDefaultValue(string.Empty).IsRequired();
        builder.Property(x => x.IsPob).HasColumnName("is_pob").HasDefaultValue(false).IsRequired();

        builder.HasOne(x => x.Product)
            .WithOne(x => x.PhotocardDetail)
            .HasForeignKey<PhotocardDetail>(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
