using CMMS.Core.Entities;
using CMMS.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMMS.Infrastructure.Data.Configurations;

public class WorkOrderLaborConfiguration : IEntityTypeConfiguration<WorkOrderLabor>
{
    public void Configure(EntityTypeBuilder<WorkOrderLabor> builder)
    {
        builder.ToTable("work_order_labor", "maintenance");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).HasColumnName("id");

        builder.Property(l => l.WorkOrderId)
            .HasColumnName("work_order_id")
            .IsRequired();

        builder.Property(l => l.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(l => l.WorkDate)
            .HasColumnName("work_date")
            .IsRequired();

        builder.Property(l => l.HoursWorked)
            .HasColumnName("hours_worked")
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(l => l.LaborType)
            .HasColumnName("labor_type")
            .HasMaxLength(20)
            .HasConversion(v => v.ToString(), v => Enum.Parse<LaborType>(v))
            .HasDefaultValue(LaborType.Regular);

        builder.Property(l => l.HourlyRate)
            .HasColumnName("hourly_rate")
            .HasPrecision(10, 2);

        builder.Property(l => l.Notes)
            .HasColumnName("notes")
            .HasMaxLength(1000);

        builder.Property(l => l.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql(SqlDialect.UtcNow());

        // Relationships
        builder.HasOne(l => l.User)
            .WithMany()
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(l => l.WorkOrderId);
        builder.HasIndex(l => l.UserId);
        builder.HasIndex(l => l.WorkDate);
    }
}
