using CMMS.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMMS.Infrastructure.Data.Configurations;

public class WorkOrderTaskConfiguration : IEntityTypeConfiguration<WorkOrderTask>
{
    public void Configure(EntityTypeBuilder<WorkOrderTask> builder)
    {
        builder.ToTable("work_order_tasks", "maintenance");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id");

        builder.Property(t => t.WorkOrderId)
            .HasColumnName("work_order_id")
            .IsRequired();

        builder.Property(t => t.SortOrder)
            .HasColumnName("sort_order")
            .HasDefaultValue(0);

        builder.Property(t => t.Description)
            .HasColumnName("description")
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(t => t.IsCompleted)
            .HasColumnName("is_completed")
            .HasDefaultValue(false);

        builder.Property(t => t.CompletedAt)
            .HasColumnName("completed_at");

        builder.Property(t => t.CompletedById)
            .HasColumnName("completed_by_id");

        builder.Property(t => t.Notes)
            .HasColumnName("notes")
            .HasMaxLength(2000);

        builder.Property(t => t.IsRequired)
            .HasColumnName("is_required")
            .HasDefaultValue(true);

        builder.Property(t => t.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql(SqlDialect.UtcNow());

        // Relationships
        builder.HasOne(t => t.WorkOrder)
            .WithMany(w => w.Tasks)
            .HasForeignKey(t => t.WorkOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.CompletedBy)
            .WithMany()
            .HasForeignKey(t => t.CompletedById)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(t => t.WorkOrderId);
    }
}
