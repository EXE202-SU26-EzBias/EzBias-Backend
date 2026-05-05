using EzBias.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EzBias.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.TokenHash).HasColumnName("token_hash").HasColumnType("text").IsRequired();
        builder.Property(x => x.IsRevoked).HasColumnName("is_revoked").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.ExpiresAt).HasColumnName("expires_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.DeviceInfo).HasColumnName("device_info").HasColumnType("text");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.RevokedAt).HasColumnName("revoked_at").HasColumnType("timestamptz");

        builder.HasOne(x => x.User)
            .WithMany(x => x.RefreshTokens)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.TokenHash).IsUnique().HasDatabaseName("idx_refresh_tokens_hash_active");
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.ExpiresAt).HasDatabaseName("idx_refresh_tokens_expires_at");
    }
}
