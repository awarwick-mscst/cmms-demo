using CMMS.Core.Entities;
using CMMS.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMMS.Infrastructure.Data.Configurations;

public class PreventiveMaintenanceScheduleConfiguration : IEntityTypeConfiguration<PreventiveMaintenanceSchedule>
{
    public void Configure(EntityTypeBuilder<PreventiveMaintenanceSchedule> builder)
    {
        builder.ToTable("preventive_maintenance_schedules", "maintenance");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id");

        builder.Property(p => p.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.Description)
            .HasColumnName("description")
            .HasMaxLength(4000);

        builder.Property(p => p.AssetId)
            .HasColumnName("asset_id");

        builder.Property(p => p.FrequencyType)
            .HasColumnName("frequency_type")
            .HasMaxLength(20)
            .HasConversion(v => v.ToString(), v => Enum.Parse<FrequencyType>(v))
            .IsRequired();

        builder.Property(p => p.FrequencyValue)
            .HasColumnName("frequency_value")
            .HasDefaultValue(1);

        builder.Property(p => p.DayOfWeek)
            .HasColumnName("day_of_week");

        builder.Property(p => p.DayOfMonth)
            .HasColumnName("day_of_month");

        builder.Property(p => p.NextDueDate)
            .HasColumnName("next_due_date");

        builder.Property(p => p.LastCompletedDate)
            .HasColumnName("last_completed_date");

        builder.Property(p => p.LeadTimeDays)
            .HasColumnName("lead_time_days")
            .HasDefaultValue(0);

        builder.Property(p => p.WorkOrderTitle)
            .HasColumnName("work_order_title")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.WorkOrderDescription)
            .HasColumnName("work_order_description")
            .HasMaxLength(4000);

        builder.Property(p => p.Priority)
            .HasColumnName("priority")
            .HasMaxLength(20)
            .HasConversion(v => v.ToString(), v => Enum.Parse<WorkOrderPriority>(v))
            .HasDefaultValue(WorkOrderPriority.Medium);

        builder.Property(p => p.EstimatedHours)
            .HasColumnName("estimated_hours")
            .HasPrecision(10, 2);

        builder.Property(p => p.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(p => p.TaskTemplateId)
            .HasColumnName("task_template_id");

        // Audit fields
        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql(SqlDialect.UtcNow());

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
        builder.HasOne(p => p.Asset)
            .WithMany()
            .HasForeignKey(p => p.AssetId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(p => p.TaskTemplate)
            .WithMany(t => t.PreventiveMaintenanceSchedules)
            .HasForeignKey(p => p.TaskTemplateId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(p => p.AssetId);
        builder.HasIndex(p => p.NextDueDate);
        builder.HasIndex(p => p.IsActive);

        // Soft delete filter
        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}
