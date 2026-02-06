using CMMS.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMMS.Infrastructure.Data.Configurations;

public class WorkOrderTaskTemplateConfiguration : IEntityTypeConfiguration<WorkOrderTaskTemplate>
{
    public void Configure(EntityTypeBuilder<WorkOrderTaskTemplate> builder)
    {
        builder.ToTable("work_order_task_templates", "maintenance");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id");

        builder.Property(t => t.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(t => t.Description)
            .HasColumnName("description")
            .HasMaxLength(2000);

        builder.Property(t => t.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        // Audit fields
        builder.Property(t => t.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(t => t.CreatedBy)
            .HasColumnName("created_by");

        builder.Property(t => t.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(t => t.UpdatedBy)
            .HasColumnName("updated_by");

        builder.Property(t => t.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValue(false);

        builder.Property(t => t.DeletedAt)
            .HasColumnName("deleted_at");

        // Relationships
        builder.HasMany(t => t.Items)
            .WithOne(i => i.Template)
            .HasForeignKey(i => i.TemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        // Soft delete filter
        builder.HasQueryFilter(t => !t.IsDeleted);
    }
}
