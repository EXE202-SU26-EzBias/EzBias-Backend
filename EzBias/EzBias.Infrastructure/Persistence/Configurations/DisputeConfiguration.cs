using EzBias.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EzBias.Infrastructure.Persistence.Configurations;

public sealed class DisputeConfiguration : IEntityTypeConfiguration<Dispute>
{
    public void Configure(EntityTypeBuilder<Dispute> builder)
    {
        builder.ToTable("disputes");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(x => x.OrderId).HasColumnName("order_id").IsRequired();
        builder.Property(x => x.InitiatorId).HasColumnName("initiator_id").IsRequired();
        builder.Property(x => x.Reason).HasColumnName("reason").HasColumnType("text").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasColumnType("text").HasDefaultValue(Domain.Enums.DisputeStatus.Open).IsRequired();
        builder.Property(x => x.AdminNote).HasColumnName("admin_note").HasColumnType("text");
        builder.Property(x => x.ResolvedAt).HasColumnName("resolved_at").HasColumnType("timestamptz");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").HasDefaultValueSql("now()").IsRequired();

        builder.HasOne(x => x.Order)
            .WithOne(x => x.Dispute)
            .HasForeignKey<Dispute>(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Initiator)
            .WithMany(x => x.DisputesOpened)
            .HasForeignKey(x => x.InitiatorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.OrderId).IsUnique();
        builder.HasIndex(x => x.Status).HasDatabaseName("idx_disputes_open");
    }
}
