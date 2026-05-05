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

        builder.HasOne(x => x.User)
            .WithMany(x => x.Notifications)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.UserId, x.IsRead, x.CreatedAt }).HasDatabaseName("idx_notifications_unread");
    }
}
