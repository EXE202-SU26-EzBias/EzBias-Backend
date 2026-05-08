using EzBias.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EzBias.Infrastructure.Persistence.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.SellerId).HasColumnName("seller_id").IsRequired();
        builder.Property(x => x.Source).HasColumnName("source").HasConversion<string>().HasColumnType("text").HasDefaultValue(Domain.Enums.OrderSource.Cart).IsRequired();
        builder.Property(x => x.AuctionId).HasColumnName("auction_id");

        builder.Property(x => x.Total).HasColumnName("total").HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasColumnType("text").HasDefaultValue(Domain.Enums.OrderStatus.Pending).IsRequired();
        builder.Property(x => x.AddressSnap).HasColumnName("address_snap").HasColumnType("jsonb").HasDefaultValue("{}").IsRequired();

        builder.Property(x => x.Carrier).HasColumnName("carrier").HasColumnType("text");
        builder.Property(x => x.TrackingNumber).HasColumnName("tracking_number").HasColumnType("text");
        builder.Property(x => x.ShippedAt).HasColumnName("shipped_at").HasColumnType("timestamptz");
        builder.Property(x => x.DeliveredAt).HasColumnName("delivered_at").HasColumnType("timestamptz");
        builder.Property(x => x.CompletedAt).HasColumnName("completed_at").HasColumnType("timestamptz");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");

        builder.HasOne(x => x.User)
            .WithMany(x => x.OrdersAsBuyer)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Seller)
            .WithMany(x => x.OrdersAsSeller)
            .HasForeignKey(x => x.SellerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Auction)
            .WithMany(x => x.Orders)
            .HasForeignKey(x => x.AuctionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => new { x.UserId, x.CreatedAt });
        builder.HasIndex(x => new { x.SellerId, x.CreatedAt });
        builder.HasIndex(x => new { x.SellerId, x.Status }).HasDatabaseName("idx_orders_seller_status");
        builder.HasIndex(x => new { x.UserId, x.Status }).HasDatabaseName("idx_orders_user_status");
        builder.HasIndex(x => x.AuctionId).HasDatabaseName("idx_orders_auction_id");
    }
}
