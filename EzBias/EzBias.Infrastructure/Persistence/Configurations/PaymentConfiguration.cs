using EzBias.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EzBias.Infrastructure.Persistence.Configurations;

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.Type).HasColumnName("type").HasConversion<string>().HasColumnType("text").IsRequired();
        builder.Property(x => x.Amount).HasColumnName("amount").HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(x => x.Currency).HasColumnName("currency").HasColumnType("char(3)").HasDefaultValue("VND").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasColumnType("text").HasDefaultValue(Domain.Enums.PaymentStatus.Pending).IsRequired();
        builder.Property(x => x.Reference).HasColumnName("reference").HasColumnType("text").HasDefaultValue(string.Empty).IsRequired();

        builder.Property(x => x.TransferContent).HasColumnName("transfer_content").HasColumnType("text");
        builder.Property(x => x.ProviderTxnId).HasColumnName("provider_txn_id").HasColumnType("text");
        builder.Property(x => x.Payload).HasColumnName("payload").HasColumnType("jsonb").HasDefaultValue("{}").IsRequired();
        builder.Property(x => x.PaidAt).HasColumnName("paid_at").HasColumnType("timestamptz");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");

        builder.HasOne(x => x.User)
            .WithMany(x => x.Payments)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.Status).HasDatabaseName("idx_payments_status_pending");
        builder.HasIndex(x => x.TransferContent).HasDatabaseName("idx_payments_transfer_content");
        builder.HasIndex(x => x.ProviderTxnId).HasDatabaseName("idx_payments_provider_txn_id");
    }
}
