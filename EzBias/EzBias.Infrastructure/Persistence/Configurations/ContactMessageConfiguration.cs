using EzBias.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EzBias.Infrastructure.Persistence.Configurations;

public sealed class ContactMessageConfiguration : IEntityTypeConfiguration<ContactMessage>
{
    public void Configure(EntityTypeBuilder<ContactMessage> builder)
    {
        builder.ToTable("contact_messages");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(x => x.Name).HasColumnName("name").HasColumnType("text").IsRequired();
        builder.Property(x => x.Email).HasColumnName("email").HasColumnType("text").IsRequired();
        builder.Property(x => x.Subject).HasColumnName("subject").HasColumnType("text").HasDefaultValue(string.Empty).IsRequired();
        builder.Property(x => x.Message).HasColumnName("message").HasColumnType("text").IsRequired();
        builder.Property(x => x.IsRead).HasColumnName("is_read").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").HasDefaultValueSql("now()").IsRequired();

        builder.HasIndex(x => x.CreatedAt).HasDatabaseName("idx_contact_messages_unread");
    }
}
