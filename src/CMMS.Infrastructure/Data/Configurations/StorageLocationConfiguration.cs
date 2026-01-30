using CMMS.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMMS.Infrastructure.Data.Configurations;

public class StorageLocationConfiguration : IEntityTypeConfiguration<StorageLocation>
{
    public void Configure(EntityTypeBuilder<StorageLocation> builder)
    {
        builder.ToTable("storage_locations", "inventory");

        builder.HasKey(sl => sl.Id);
        builder.Property(sl => sl.Id).HasColumnName("id");

        builder.Property(sl => sl.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(sl => sl.Code)
            .HasColumnName("code")
            .HasMaxLength(50);

        builder.Property(sl => sl.Description)
            .HasColumnName("description")
            .HasMaxLength(500);

        builder.Property(sl => sl.ParentId)
            .HasColumnName("parent_id");

        builder.Property(sl => sl.Level)
            .HasColumnName("level")
            .HasDefaultValue(0);

        builder.Property(sl => sl.FullPath)
            .HasColumnName("full_path")
            .HasMaxLength(500);

        builder.Property(sl => sl.Building)
            .HasColumnName("building")
            .HasMaxLength(100);

        builder.Property(sl => sl.Aisle)
            .HasColumnName("aisle")
            .HasMaxLength(50);

        builder.Property(sl => sl.Rack)
            .HasColumnName("rack")
            .HasMaxLength(50);

        builder.Property(sl => sl.Shelf)
            .HasColumnName("shelf")
            .HasMaxLength(50);

        builder.Property(sl => sl.Bin)
            .HasColumnName("bin")
            .HasMaxLength(50);

        builder.Property(sl => sl.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(sl => sl.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(sl => sl.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(sl => sl.CreatedBy)
            .HasColumnName("created_by");

        builder.Property(sl => sl.UpdatedBy)
            .HasColumnName("updated_by");

        builder.Property(sl => sl.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValue(false);

        builder.Property(sl => sl.DeletedAt)
            .HasColumnName("deleted_at");

        builder.HasOne(sl => sl.Parent)
            .WithMany(sl => sl.Children)
            .HasForeignKey(sl => sl.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(sl => sl.Code)
            .IsUnique()
            .HasFilter("[is_deleted] = 0 AND [code] IS NOT NULL");

        builder.HasIndex(sl => sl.ParentId);

        builder.HasQueryFilter(sl => !sl.IsDeleted);
    }
}
