using EzBias.Domain.Entities;
using EzBias.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EzBias.Infrastructure.Persistence.Configurations;

public sealed class CallSessionConfiguration : IEntityTypeConfiguration<CallSession>
{
    public void Configure(EntityTypeBuilder<CallSession> builder)
    {
        builder.ToTable("call_sessions");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(x => x.ConversationId).HasColumnName("conversation_id").IsRequired();
        builder.Property(x => x.CallerId).HasColumnName("caller_id").IsRequired();
        builder.Property(x => x.CalleeId).HasColumnName("callee_id").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasColumnType("text").HasDefaultValue(CallSessionStatus.Ringing).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.AnsweredAt).HasColumnName("answered_at").HasColumnType("timestamptz");
        builder.Property(x => x.EndedAt).HasColumnName("ended_at").HasColumnType("timestamptz");

        builder.HasOne(x => x.Conversation)
            .WithMany()
            .HasForeignKey(x => x.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Caller)
            .WithMany()
            .HasForeignKey(x => x.CallerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Callee)
            .WithMany()
            .HasForeignKey(x => x.CalleeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ConversationId, x.CreatedAt });
        builder.HasIndex(x => new { x.CallerId, x.Status });
        builder.HasIndex(x => new { x.CalleeId, x.Status });
    }
}
