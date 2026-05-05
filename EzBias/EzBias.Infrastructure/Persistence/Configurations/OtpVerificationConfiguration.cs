using EzBias.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EzBias.Infrastructure.Persistence.Configurations;

public sealed class OtpVerificationConfiguration : IEntityTypeConfiguration<OtpVerification>
{
    public void Configure(EntityTypeBuilder<OtpVerification> builder)
    {
        builder.ToTable("otp_verifications");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.Channel).HasColumnName("channel").HasConversion<string>().HasColumnType("text").IsRequired();
        builder.Property(x => x.Purpose).HasColumnName("purpose").HasConversion<string>().HasColumnType("text").IsRequired();
        builder.Property(x => x.CodeHash).HasColumnName("code_hash").HasColumnType("text").IsRequired();
        builder.Property(x => x.IsUsed).HasColumnName("is_used").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.ExpiresAt).HasColumnName("expires_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").HasDefaultValueSql("now()").IsRequired();

        builder.HasOne(x => x.User)
            .WithMany(x => x.OtpVerifications)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.UserId, x.Purpose }).HasDatabaseName("idx_otp_active");
        builder.HasIndex(x => x.ExpiresAt).HasDatabaseName("idx_otp_cleanup");
    }
}
