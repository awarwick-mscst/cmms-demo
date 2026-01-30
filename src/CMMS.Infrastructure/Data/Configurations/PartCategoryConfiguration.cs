using CMMS.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMMS.Infrastructure.Data.Configurations;

public class PartCategoryConfiguration : IEntityTypeConfiguration<PartCategory>
{
    public void Configure(EntityTypeBuilder<PartCategory> builder)
    {
        builder.ToTable("part_categories", "inventory");

        builder.HasKey(pc => pc.Id);
        builder.Property(pc => pc.Id).HasColumnName("id");

        builder.Property(pc => pc.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(pc => pc.Code)
            .HasColumnName("code")
            .HasMaxLength(50);

        builder.Property(pc => pc.Description)
            .HasColumnName("description")
            .HasMaxLength(500);

        builder.Property(pc => pc.ParentId)
            .HasColumnName("parent_id");

        builder.Property(pc => pc.Level)
            .HasColumnName("level")
            .HasDefaultValue(0);

        builder.Property(pc => pc.SortOrder)
            .HasColumnName("sort_order")
            .HasDefaultValue(0);

        builder.Property(pc => pc.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(pc => pc.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(pc => pc.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(pc => pc.CreatedBy)
            .HasColumnName("created_by");

        builder.Property(pc => pc.UpdatedBy)
            .HasColumnName("updated_by");

        builder.Property(pc => pc.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValue(false);

        builder.Property(pc => pc.DeletedAt)
            .HasColumnName("deleted_at");

        builder.HasOne(pc => pc.Parent)
            .WithMany(pc => pc.Children)
            .HasForeignKey(pc => pc.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(pc => pc.Code)
            .IsUnique()
            .HasFilter("[is_deleted] = 0 AND [code] IS NOT NULL");

        builder.HasIndex(pc => new { pc.ParentId, pc.SortOrder });

        builder.HasQueryFilter(pc => !pc.IsDeleted);
    }
}
