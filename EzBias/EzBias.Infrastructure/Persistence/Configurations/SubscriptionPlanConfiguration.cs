using EzBias.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EzBias.Infrastructure.Persistence.Configurations;

public sealed class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlan>
{
    public void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
    {
        builder.ToTable("subscription_plans");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id").HasColumnType("text").ValueGeneratedNever();
        builder.Property(x => x.Name).HasColumnName("name").HasColumnType("text").IsRequired();
        builder.Property(x => x.Price).HasColumnName("price").HasColumnType("numeric(18,2)").HasDefaultValue(0m).IsRequired();
        builder.Property(x => x.DurationDays).HasColumnName("duration_days").HasDefaultValue(0).IsRequired();
        builder.Property(x => x.DurationHours).HasColumnName("duration_hours").HasDefaultValue(0).IsRequired();
        builder.Property(x => x.Features).HasColumnName("features").HasColumnType("jsonb").HasDefaultValue("{}").IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");
    }
}
