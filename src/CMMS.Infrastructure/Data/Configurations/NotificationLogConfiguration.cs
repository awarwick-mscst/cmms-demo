using CMMS.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMMS.Infrastructure.Data.Configurations;

public class NotificationLogConfiguration : IEntityTypeConfiguration<NotificationLog>
{
    public void Configure(EntityTypeBuilder<NotificationLog> builder)
    {
        builder.ToTable("notification_log", "core");

        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).HasColumnName("id");

        builder.Property(n => n.Type)
            .HasColumnName("type")
            .IsRequired();

        builder.Property(n => n.RecipientEmail)
            .HasColumnName("recipient_email")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(n => n.Subject)
            .HasColumnName("subject")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(n => n.Channel)
            .HasColumnName("channel")
            .IsRequired();

        builder.Property(n => n.Success)
            .HasColumnName("success")
            .IsRequired();

        builder.Property(n => n.ExternalMessageId)
            .HasColumnName("external_message_id")
            .HasMaxLength(256);

        builder.Property(n => n.ErrorMessage)
            .HasColumnName("error_message");

        builder.Property(n => n.SentAt)
            .HasColumnName("sent_at")
            .HasDefaultValueSql(SqlDialect.UtcNow());

        builder.Property(n => n.QueueId)
            .HasColumnName("queue_id");

        builder.Property(n => n.ReferenceType)
            .HasColumnName("reference_type")
            .HasMaxLength(50);

        builder.Property(n => n.ReferenceId)
            .HasColumnName("reference_id");

        builder.Property(n => n.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql(SqlDialect.UtcNow());

        // Relationships
        builder.HasOne(n => n.Queue)
            .WithMany()
            .HasForeignKey(n => n.QueueId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(n => n.SentAt);
        builder.HasIndex(n => n.Type);
        builder.HasIndex(n => new { n.ReferenceType, n.ReferenceId });
    }
}
