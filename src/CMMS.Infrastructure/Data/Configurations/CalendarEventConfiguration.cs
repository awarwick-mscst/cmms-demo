using CMMS.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMMS.Infrastructure.Data.Configurations;

public class CalendarEventConfiguration : IEntityTypeConfiguration<CalendarEvent>
{
    public void Configure(EntityTypeBuilder<CalendarEvent> builder)
    {
        builder.ToTable("calendar_events", "core");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");

        builder.Property(e => e.ExternalEventId)
            .HasColumnName("external_event_id")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(e => e.CalendarType)
            .HasColumnName("calendar_type")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.UserId)
            .HasColumnName("user_id");

        builder.Property(e => e.ReferenceType)
            .HasColumnName("reference_type")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.ReferenceId)
            .HasColumnName("reference_id")
            .IsRequired();

        builder.Property(e => e.Title)
            .HasColumnName("title")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.StartTime)
            .HasColumnName("start_time")
            .IsRequired();

        builder.Property(e => e.EndTime)
            .HasColumnName("end_time")
            .IsRequired();

        builder.Property(e => e.ProviderType)
            .HasColumnName("provider_type")
            .HasMaxLength(50)
            .HasDefaultValue("MicrosoftGraph");

        // Audit fields
        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(e => e.CreatedBy)
            .HasColumnName("created_by");

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(e => e.UpdatedBy)
            .HasColumnName("updated_by");

        builder.Property(e => e.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValue(false);

        builder.Property(e => e.DeletedAt)
            .HasColumnName("deleted_at");

        // Relationships
        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(e => e.ExternalEventId)
            .HasFilter("[is_deleted] = 0");

        builder.HasIndex(e => new { e.ReferenceType, e.ReferenceId })
            .HasFilter("[is_deleted] = 0");

        builder.HasIndex(e => e.UserId)
            .HasFilter("[is_deleted] = 0");

        // Soft delete filter
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
