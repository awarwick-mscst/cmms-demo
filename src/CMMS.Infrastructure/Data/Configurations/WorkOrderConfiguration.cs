using CMMS.Core.Entities;
using CMMS.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMMS.Infrastructure.Data.Configurations;

public class WorkOrderConfiguration : IEntityTypeConfiguration<WorkOrder>
{
    public void Configure(EntityTypeBuilder<WorkOrder> builder)
    {
        builder.ToTable("work_orders", "maintenance");

        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id).HasColumnName("id");

        builder.Property(w => w.WorkOrderNumber)
            .HasColumnName("work_order_number")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(w => w.Type)
            .HasColumnName("type")
            .HasMaxLength(30)
            .HasConversion(v => v.ToString(), v => Enum.Parse<WorkOrderType>(v))
            .IsRequired();

        builder.Property(w => w.Priority)
            .HasColumnName("priority")
            .HasMaxLength(20)
            .HasConversion(v => v.ToString(), v => Enum.Parse<WorkOrderPriority>(v))
            .HasDefaultValue(WorkOrderPriority.Medium);

        builder.Property(w => w.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasConversion(v => v.ToString(), v => Enum.Parse<WorkOrderStatus>(v))
            .HasDefaultValue(WorkOrderStatus.Draft);

        builder.Property(w => w.Title)
            .HasColumnName("title")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(w => w.Description)
            .HasColumnName("description")
            .HasMaxLength(4000);

        builder.Property(w => w.AssetId)
            .HasColumnName("asset_id");

        builder.Property(w => w.LocationId)
            .HasColumnName("location_id");

        builder.Property(w => w.RequestedBy)
            .HasColumnName("requested_by")
            .HasMaxLength(200);

        builder.Property(w => w.RequestedDate)
            .HasColumnName("requested_date");

        builder.Property(w => w.AssignedToId)
            .HasColumnName("assigned_to_id");

        builder.Property(w => w.ScheduledStartDate)
            .HasColumnName("scheduled_start_date");

        builder.Property(w => w.ScheduledEndDate)
            .HasColumnName("scheduled_end_date");

        builder.Property(w => w.ActualStartDate)
            .HasColumnName("actual_start_date");

        builder.Property(w => w.ActualEndDate)
            .HasColumnName("actual_end_date");

        builder.Property(w => w.EstimatedHours)
            .HasColumnName("estimated_hours")
            .HasPrecision(10, 2);

        builder.Property(w => w.ActualHours)
            .HasColumnName("actual_hours")
            .HasPrecision(10, 2);

        builder.Property(w => w.CompletionNotes)
            .HasColumnName("completion_notes")
            .HasMaxLength(4000);

        builder.Property(w => w.PreventiveMaintenanceScheduleId)
            .HasColumnName("preventive_maintenance_schedule_id");

        // Audit fields
        builder.Property(w => w.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(w => w.CreatedBy)
            .HasColumnName("created_by");

        builder.Property(w => w.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(w => w.UpdatedBy)
            .HasColumnName("updated_by");

        builder.Property(w => w.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValue(false);

        builder.Property(w => w.DeletedAt)
            .HasColumnName("deleted_at");

        // Relationships
        builder.HasOne(w => w.Asset)
            .WithMany()
            .HasForeignKey(w => w.AssetId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(w => w.Location)
            .WithMany()
            .HasForeignKey(w => w.LocationId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(w => w.AssignedTo)
            .WithMany()
            .HasForeignKey(w => w.AssignedToId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(w => w.PreventiveMaintenanceSchedule)
            .WithMany(pm => pm.GeneratedWorkOrders)
            .HasForeignKey(w => w.PreventiveMaintenanceScheduleId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(w => w.History)
            .WithOne(h => h.WorkOrder)
            .HasForeignKey(h => h.WorkOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(w => w.Comments)
            .WithOne(c => c.WorkOrder)
            .HasForeignKey(c => c.WorkOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(w => w.LaborEntries)
            .WithOne(l => l.WorkOrder)
            .HasForeignKey(l => l.WorkOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(w => w.Parts)
            .WithOne(p => p.WorkOrder)
            .HasForeignKey(p => p.WorkOrderId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(w => w.WorkOrderNumber)
            .IsUnique()
            .HasFilter("[is_deleted] = 0");

        builder.HasIndex(w => w.Status);
        builder.HasIndex(w => w.Type);
        builder.HasIndex(w => w.Priority);
        builder.HasIndex(w => w.AssetId);
        builder.HasIndex(w => w.AssignedToId);
        builder.HasIndex(w => w.ScheduledStartDate);

        // Soft delete filter
        builder.HasQueryFilter(w => !w.IsDeleted);
    }
}
