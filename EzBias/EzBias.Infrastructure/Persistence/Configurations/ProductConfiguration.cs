using EzBias.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EzBias.Infrastructure.Persistence.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(x => x.SellerId).HasColumnName("seller_id").IsRequired();
        builder.Property(x => x.FandomId).HasColumnName("fandom_id").HasColumnType("text").IsRequired();

        builder.Property(x => x.Artist).HasColumnName("artist").HasColumnType("text").HasDefaultValue(string.Empty).IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasColumnType("text").HasDefaultValue(string.Empty).IsRequired();
        builder.Property(x => x.Type).HasColumnName("type").HasColumnType("text").HasDefaultValue(string.Empty).IsRequired();
        builder.Property(x => x.Condition).HasColumnName("condition").HasConversion<string>().HasColumnType("text").HasDefaultValue(Domain.Enums.ProductCondition.Good).IsRequired();
        builder.Property(x => x.Price).HasColumnName("price").HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(x => x.Stock).HasColumnName("stock").HasDefaultValue(0).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description").HasColumnType("text").HasDefaultValue(string.Empty).IsRequired();
        builder.Property(x => x.PrimaryImageUrl).HasColumnName("primary_image_url").HasColumnType("text").HasDefaultValue(string.Empty).IsRequired();

        builder.Property(x => x.IsAuction).HasColumnName("is_auction").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasColumnType("text").HasDefaultValue(Domain.Enums.ProductStatus.Active).IsRequired();
        builder.Property(x => x.Version).HasColumnName("version").HasDefaultValue(0).IsRequired();

        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamptz");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");

        builder.HasOne(x => x.Seller)
            .WithMany(x => x.Products)
            .HasForeignKey(x => x.SellerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Fandom)
            .WithMany(x => x.Products)
            .HasForeignKey(x => x.FandomId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.SellerId);
        builder.HasIndex(x => x.FandomId);
        builder.HasIndex(x => x.Type);
        builder.HasIndex(x => new { x.FandomId, x.CreatedAt }).HasDatabaseName("idx_products_browse");
    }
}
