using EzBias.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EzBias.Infrastructure.Persistence.Configurations;

public sealed class DisputeItemConfiguration : IEntityTypeConfiguration<DisputeItem>
{
    public void Configure(EntityTypeBuilder<DisputeItem> builder)
    {
        builder.ToTable("dispute_items");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(x => x.DisputeId).HasColumnName("dispute_id").IsRequired();
        builder.Property(x => x.OrderItemId).HasColumnName("order_item_id").IsRequired();
        builder.Property(x => x.RequestedQty).HasColumnName("requested_qty").IsRequired();
        builder.Property(x => x.ApprovedQty).HasColumnName("approved_qty");
        builder.Property(x => x.Note).HasColumnName("note").HasColumnType("text");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").HasDefaultValueSql("now()").IsRequired();

        builder.HasOne(x => x.Dispute)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.DisputeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.OrderItem)
            .WithMany(x => x.DisputeItems)
            .HasForeignKey(x => x.OrderItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.DisputeId);
        builder.HasIndex(x => x.OrderItemId);
        builder.HasIndex(x => new { x.DisputeId, x.OrderItemId }).IsUnique();
    }
}
