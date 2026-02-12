using CMMS.Core.Entities;
using CMMS.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMMS.Infrastructure.Data.Configurations;

public class NotificationQueueConfiguration : IEntityTypeConfiguration<NotificationQueue>
{
    public void Configure(EntityTypeBuilder<NotificationQueue> builder)
    {
        builder.ToTable("notification_queue", "core");

        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).HasColumnName("id");

        builder.Property(n => n.Type)
            .HasColumnName("type")
            .IsRequired();

        builder.Property(n => n.RecipientUserId)
            .HasColumnName("recipient_user_id");

        builder.Property(n => n.RecipientEmail)
            .HasColumnName("recipient_email")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(n => n.Subject)
            .HasColumnName("subject")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(n => n.Body)
            .HasColumnName("body")
            .IsRequired();

        builder.Property(n => n.BodyHtml)
            .HasColumnName("body_html");

        builder.Property(n => n.Status)
            .HasColumnName("status")
            .HasDefaultValue(NotificationStatus.Pending);

        builder.Property(n => n.RetryCount)
            .HasColumnName("retry_count")
            .HasDefaultValue(0);

        builder.Property(n => n.ScheduledFor)
            .HasColumnName("scheduled_for")
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(n => n.ProcessedAt)
            .HasColumnName("processed_at");

        builder.Property(n => n.ErrorMessage)
            .HasColumnName("error_message");

        builder.Property(n => n.ReferenceType)
            .HasColumnName("reference_type")
            .HasMaxLength(50);

        builder.Property(n => n.ReferenceId)
            .HasColumnName("reference_id");

        // Audit fields
        builder.Property(n => n.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(n => n.CreatedBy)
            .HasColumnName("created_by");

        builder.Property(n => n.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(n => n.UpdatedBy)
            .HasColumnName("updated_by");

        builder.Property(n => n.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValue(false);

        builder.Property(n => n.DeletedAt)
            .HasColumnName("deleted_at");

        // Relationships
        builder.HasOne(n => n.RecipientUser)
            .WithMany()
            .HasForeignKey(n => n.RecipientUserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(n => new { n.Status, n.ScheduledFor })
            .HasFilter("[is_deleted] = 0");

        builder.HasIndex(n => n.RecipientUserId)
            .HasFilter("[is_deleted] = 0");

        builder.HasIndex(n => new { n.ReferenceType, n.ReferenceId })
            .HasFilter("[is_deleted] = 0");

        // Soft delete filter
        builder.HasQueryFilter(n => !n.IsDeleted);
    }
}
