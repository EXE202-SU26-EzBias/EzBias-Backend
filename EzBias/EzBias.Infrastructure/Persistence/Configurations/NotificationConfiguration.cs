using EzBias.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EzBias.Infrastructure.Persistence.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.Type).HasColumnName("type").HasConversion<string>().HasColumnType("text").IsRequired();
        builder.Property(x => x.Title).HasColumnName("title").HasColumnType("text").IsRequired();
        builder.Property(x => x.Body).HasColumnName("body").HasColumnType("text").IsRequired();
        builder.Property(x => x.Meta).HasColumnName("meta").HasColumnType("jsonb").HasDefaultValue("{}").IsRequired();
        builder.Property(x => x.IsRead).HasColumnName("is_read").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.ReadAt).HasColumnName("read_at").HasColumnType("timestamptz");
        builder.Property(x => x.DispatchedAt).HasColumnName("dispatched_at").HasColumnType("timestamptz");
        builder.Property(x => x.DispatchAttempts).HasColumnName("dispatch_attempts").HasDefaultValue(0).IsRequired();
        builder.Property(x => x.NextDispatchAt).HasColumnName("next_dispatch_at").HasColumnType("timestamptz");
        builder.Property(x => x.DispatchLeaseId).HasColumnName("dispatch_lease_id").HasColumnType("uuid");
        builder.Property(x => x.DispatchLockedUntil).HasColumnName("dispatch_locked_until").HasColumnType("timestamptz");
        builder.Property(x => x.LastDispatchError).HasColumnName("last_dispatch_error").HasColumnType("text");
        builder.Property(x => x.DispatchFailedAt).HasColumnName("dispatch_failed_at").HasColumnType("timestamptz");

        builder.HasOne(x => x.User)
            .WithMany(x => x.Notifications)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.UserId, x.IsRead, x.CreatedAt }).HasDatabaseName("idx_notifications_unread");
        builder.HasIndex(x => new { x.NextDispatchAt, x.CreatedAt })
            .HasDatabaseName("idx_notifications_dispatch_pending")
            .HasFilter("dispatched_at IS NULL AND dispatch_failed_at IS NULL");
    }
}
