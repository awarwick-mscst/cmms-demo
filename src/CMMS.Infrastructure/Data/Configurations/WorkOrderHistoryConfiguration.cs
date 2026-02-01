using CMMS.Core.Entities;
using CMMS.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMMS.Infrastructure.Data.Configurations;

public class WorkOrderHistoryConfiguration : IEntityTypeConfiguration<WorkOrderHistory>
{
    public void Configure(EntityTypeBuilder<WorkOrderHistory> builder)
    {
        builder.ToTable("work_order_history", "maintenance");

        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).HasColumnName("id");

        builder.Property(h => h.WorkOrderId)
            .HasColumnName("work_order_id")
            .IsRequired();

        builder.Property(h => h.FromStatus)
            .HasColumnName("from_status")
            .HasMaxLength(20)
            .HasConversion(
                v => v.HasValue ? v.Value.ToString() : null,
                v => v != null ? Enum.Parse<WorkOrderStatus>(v) : null);

        builder.Property(h => h.ToStatus)
            .HasColumnName("to_status")
            .HasMaxLength(20)
            .HasConversion(v => v.ToString(), v => Enum.Parse<WorkOrderStatus>(v))
            .IsRequired();

        builder.Property(h => h.ChangedById)
            .HasColumnName("changed_by_id")
            .IsRequired();

        builder.Property(h => h.ChangedAt)
            .HasColumnName("changed_at")
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(h => h.Notes)
            .HasColumnName("notes")
            .HasMaxLength(1000);

        // Relationships
        builder.HasOne(h => h.ChangedBy)
            .WithMany()
            .HasForeignKey(h => h.ChangedById)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(h => h.WorkOrderId);
        builder.HasIndex(h => h.ChangedAt);
    }
}
