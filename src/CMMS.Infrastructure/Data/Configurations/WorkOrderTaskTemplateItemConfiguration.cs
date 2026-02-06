using CMMS.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMMS.Infrastructure.Data.Configurations;

public class WorkOrderTaskTemplateItemConfiguration : IEntityTypeConfiguration<WorkOrderTaskTemplateItem>
{
    public void Configure(EntityTypeBuilder<WorkOrderTaskTemplateItem> builder)
    {
        builder.ToTable("work_order_task_template_items", "maintenance");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasColumnName("id");

        builder.Property(i => i.TemplateId)
            .HasColumnName("template_id")
            .IsRequired();

        builder.Property(i => i.SortOrder)
            .HasColumnName("sort_order")
            .HasDefaultValue(0);

        builder.Property(i => i.Description)
            .HasColumnName("description")
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(i => i.IsRequired)
            .HasColumnName("is_required")
            .HasDefaultValue(true);

        // Indexes
        builder.HasIndex(i => i.TemplateId);
    }
}
