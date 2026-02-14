using CMMS.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMMS.Infrastructure.Data.Configurations;

public class WorkOrderCommentConfiguration : IEntityTypeConfiguration<WorkOrderComment>
{
    public void Configure(EntityTypeBuilder<WorkOrderComment> builder)
    {
        builder.ToTable("work_order_comments", "maintenance");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id");

        builder.Property(c => c.WorkOrderId)
            .HasColumnName("work_order_id")
            .IsRequired();

        builder.Property(c => c.Comment)
            .HasColumnName("comment")
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(c => c.IsInternal)
            .HasColumnName("is_internal")
            .HasDefaultValue(false);

        builder.Property(c => c.CreatedById)
            .HasColumnName("created_by_id")
            .IsRequired();

        builder.Property(c => c.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql(SqlDialect.UtcNow());

        // Relationships
        builder.HasOne(c => c.CreatedBy)
            .WithMany()
            .HasForeignKey(c => c.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(c => c.WorkOrderId);
        builder.HasIndex(c => c.CreatedAt);
    }
}
