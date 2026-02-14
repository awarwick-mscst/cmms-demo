using CMMS.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMMS.Infrastructure.Data.Configurations;

public class AiMessageConfiguration : IEntityTypeConfiguration<AiMessage>
{
    public void Configure(EntityTypeBuilder<AiMessage> builder)
    {
        builder.ToTable("ai_messages", "ai");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("id");

        builder.Property(m => m.ConversationId)
            .HasColumnName("conversation_id")
            .IsRequired();

        builder.Property(m => m.Role)
            .HasColumnName("role")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(m => m.Content)
            .HasColumnName("content")
            .HasColumnType(SqlDialect.UnboundedText())
            .IsRequired();

        builder.Property(m => m.ContextType)
            .HasColumnName("context_type")
            .HasMaxLength(50);

        builder.Property(m => m.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql(SqlDialect.UtcNow());

        // Indexes
        builder.HasIndex(m => m.ConversationId);
    }
}
