using CMMS.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMMS.Infrastructure.Data.Configurations;

public class UserNotificationPreferenceConfiguration : IEntityTypeConfiguration<UserNotificationPreference>
{
    public void Configure(EntityTypeBuilder<UserNotificationPreference> builder)
    {
        builder.ToTable("user_notification_preferences", "core");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id");

        builder.Property(p => p.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(p => p.NotificationType)
            .HasColumnName("notification_type")
            .IsRequired();

        builder.Property(p => p.EmailEnabled)
            .HasColumnName("email_enabled")
            .HasDefaultValue(true);

        builder.Property(p => p.CalendarEnabled)
            .HasColumnName("calendar_enabled")
            .HasDefaultValue(true);

        // Audit fields
        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(p => p.CreatedBy)
            .HasColumnName("created_by");

        builder.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(p => p.UpdatedBy)
            .HasColumnName("updated_by");

        builder.Property(p => p.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValue(false);

        builder.Property(p => p.DeletedAt)
            .HasColumnName("deleted_at");

        // Relationships
        builder.HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint
        builder.HasIndex(p => new { p.UserId, p.NotificationType })
            .IsUnique();

        // Index
        builder.HasIndex(p => p.UserId)
            .HasFilter("[is_deleted] = 0");

        // Soft delete filter
        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}
