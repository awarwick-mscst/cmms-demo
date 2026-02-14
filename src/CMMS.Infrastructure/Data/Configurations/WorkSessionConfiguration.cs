using CMMS.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMMS.Infrastructure.Data.Configurations;

public class WorkSessionConfiguration : IEntityTypeConfiguration<WorkSession>
{
    public void Configure(EntityTypeBuilder<WorkSession> builder)
    {
        builder.ToTable("work_sessions", "maintenance");

        builder.HasKey(ws => ws.Id);
        builder.Property(ws => ws.Id).HasColumnName("id");

        builder.Property(ws => ws.WorkOrderId)
            .HasColumnName("work_order_id")
            .IsRequired();

        builder.Property(ws => ws.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(ws => ws.StartedAt)
            .HasColumnName("started_at")
            .IsRequired();

        builder.Property(ws => ws.EndedAt)
            .HasColumnName("ended_at");

        builder.Property(ws => ws.HoursWorked)
            .HasColumnName("hours_worked")
            .HasPrecision(10, 2);

        builder.Property(ws => ws.Notes)
            .HasColumnName("notes")
            .HasMaxLength(2000);

        builder.Property(ws => ws.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(ws => ws.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql(SqlDialect.UtcNow());

        builder.HasOne(ws => ws.WorkOrder)
            .WithMany()
            .HasForeignKey(ws => ws.WorkOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ws => ws.User)
            .WithMany()
            .HasForeignKey(ws => ws.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(ws => new { ws.UserId, ws.IsActive })
            .HasFilter(SqlDialect.BooleanTrueFilter("is_active"));

        builder.HasIndex(ws => new { ws.WorkOrderId, ws.IsActive });
    }
}
