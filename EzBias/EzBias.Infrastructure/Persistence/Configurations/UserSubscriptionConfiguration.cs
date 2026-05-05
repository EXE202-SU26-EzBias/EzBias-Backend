using EzBias.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EzBias.Infrastructure.Persistence.Configurations;

public sealed class UserSubscriptionConfiguration : IEntityTypeConfiguration<UserSubscription>
{
    public void Configure(EntityTypeBuilder<UserSubscription> builder)
    {
        builder.ToTable("user_subscriptions");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.PlanId).HasColumnName("plan_id").HasColumnType("text").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasColumnType("text").HasDefaultValue(Domain.Enums.SubStatus.Active).IsRequired();
        builder.Property(x => x.StartsAt).HasColumnName("starts_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.EndsAt).HasColumnName("ends_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.PaymentId).HasColumnName("payment_id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");

        builder.HasOne(x => x.User)
            .WithMany(x => x.UserSubscriptions)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Plan)
            .WithMany(x => x.UserSubscriptions)
            .HasForeignKey(x => x.PlanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Payment)
            .WithMany(x => x.UserSubscriptions)
            .HasForeignKey(x => x.PaymentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => new { x.UserId, x.EndsAt }).HasDatabaseName("idx_user_subs_active");
    }
}
