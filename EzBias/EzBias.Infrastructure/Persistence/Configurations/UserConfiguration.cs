using EzBias.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EzBias.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(x => x.FullName).HasColumnName("full_name").HasColumnType("text").HasDefaultValue(string.Empty).IsRequired();
        builder.Property(x => x.Username).HasColumnName("username").HasColumnType("text").IsRequired();
        builder.Property(x => x.Email).HasColumnName("email").HasColumnType("text").IsRequired();
        builder.Property(x => x.PasswordHash).HasColumnName("password_hash").HasColumnType("text").IsRequired();

        builder.Property(x => x.Role).HasColumnName("role").HasConversion<string>().HasColumnType("text").HasDefaultValue(Domain.Enums.UserRole.User).IsRequired();

        builder.Property(x => x.Phone).HasColumnName("phone").HasColumnType("text").HasDefaultValue(string.Empty).IsRequired();
        builder.Property(x => x.Address).HasColumnName("address").HasColumnType("text").HasDefaultValue(string.Empty).IsRequired();
        builder.Property(x => x.City).HasColumnName("city").HasColumnType("text").HasDefaultValue(string.Empty).IsRequired();
        builder.Property(x => x.Zip).HasColumnName("zip").HasColumnType("text").HasDefaultValue(string.Empty).IsRequired();
        builder.Property(x => x.AvatarUrl).HasColumnName("avatar_url").HasColumnType("text").HasDefaultValue(string.Empty).IsRequired();
        builder.Property(x => x.AvatarBg).HasColumnName("avatar_bg").HasColumnType("text").HasDefaultValue(string.Empty).IsRequired();

        builder.Property(x => x.BankName).HasColumnName("bank_name").HasColumnType("text").HasDefaultValue(string.Empty).IsRequired();
        builder.Property(x => x.BankAccountNumber).HasColumnName("bank_account_number").HasColumnType("text").HasDefaultValue(string.Empty).IsRequired();
        builder.Property(x => x.BankAccountName).HasColumnName("bank_account_name").HasColumnType("text").HasDefaultValue(string.Empty).IsRequired();

        builder.Property(x => x.EmailVerifiedAt).HasColumnName("email_verified_at").HasColumnType("timestamptz");
        builder.Property(x => x.PhoneVerifiedAt).HasColumnName("phone_verified_at").HasColumnType("timestamptz");

        builder.Property(x => x.AvgSellerRating).HasColumnName("avg_seller_rating").HasColumnType("numeric(3,2)").HasDefaultValue(0m).IsRequired();
        builder.Property(x => x.TotalRatings).HasColumnName("total_ratings").HasDefaultValue(0).IsRequired();

        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamptz");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");

        builder.HasIndex(x => x.Email).IsUnique();
        builder.HasIndex(x => x.Username).IsUnique();
    }
}
