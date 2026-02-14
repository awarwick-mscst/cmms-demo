using CMMS.Core.Entities;
using CMMS.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMMS.Infrastructure.Data.Configurations;

public class PartConfiguration : IEntityTypeConfiguration<Part>
{
    public void Configure(EntityTypeBuilder<Part> builder)
    {
        builder.ToTable("parts", "inventory");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id");

        builder.Property(p => p.PartNumber)
            .HasColumnName("part_number")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(p => p.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.Description)
            .HasColumnName("description")
            .HasMaxLength(1000);

        builder.Property(p => p.CategoryId)
            .HasColumnName("category_id");

        builder.Property(p => p.SupplierId)
            .HasColumnName("supplier_id");

        builder.Property(p => p.UnitOfMeasure)
            .HasColumnName("unit_of_measure")
            .HasMaxLength(20)
            .HasConversion(
                v => v.ToString(),
                v => Enum.Parse<UnitOfMeasure>(v))
            .HasDefaultValue(UnitOfMeasure.Each);

        builder.Property(p => p.UnitCost)
            .HasColumnName("unit_cost")
            .HasPrecision(18, 2)
            .HasDefaultValue(0);

        builder.Property(p => p.ReorderPoint)
            .HasColumnName("reorder_point")
            .HasDefaultValue(0);

        builder.Property(p => p.ReorderQuantity)
            .HasColumnName("reorder_quantity")
            .HasDefaultValue(0);

        builder.Property(p => p.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasConversion(
                v => v.ToString(),
                v => Enum.Parse<PartStatus>(v))
            .HasDefaultValue(PartStatus.Active);

        builder.Property(p => p.MinStockLevel)
            .HasColumnName("min_stock_level")
            .HasDefaultValue(0);

        builder.Property(p => p.MaxStockLevel)
            .HasColumnName("max_stock_level")
            .HasDefaultValue(0);

        builder.Property(p => p.LeadTimeDays)
            .HasColumnName("lead_time_days")
            .HasDefaultValue(0);

        builder.Property(p => p.Specifications)
            .HasColumnName("specifications")
            .HasColumnType(SqlDialect.UnboundedText());

        builder.Property(p => p.Manufacturer)
            .HasColumnName("manufacturer")
            .HasMaxLength(200);

        builder.Property(p => p.ManufacturerPartNumber)
            .HasColumnName("manufacturer_part_number")
            .HasMaxLength(100);

        builder.Property(p => p.Barcode)
            .HasColumnName("barcode")
            .HasMaxLength(100);

        builder.Property(p => p.ImageUrl)
            .HasColumnName("image_url")
            .HasMaxLength(500);

        builder.Property(p => p.Notes)
            .HasColumnName("notes")
            .HasMaxLength(2000);

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql(SqlDialect.UtcNow());

        builder.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(p => p.CreatedBy)
            .HasColumnName("created_by");

        builder.Property(p => p.UpdatedBy)
            .HasColumnName("updated_by");

        builder.Property(p => p.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValue(false);

        builder.Property(p => p.DeletedAt)
            .HasColumnName("deleted_at");

        builder.HasOne(p => p.Category)
            .WithMany(c => c.Parts)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Supplier)
            .WithMany(s => s.Parts)
            .HasForeignKey(p => p.SupplierId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(p => p.PartNumber)
            .IsUnique()
            .HasFilter(SqlDialect.SoftDeleteFilter());

        builder.HasIndex(p => p.Barcode)
            .IsUnique()
            .HasFilter(SqlDialect.SoftDeleteAndNotNullFilter("barcode"));

        builder.HasIndex(p => p.CategoryId);
        builder.HasIndex(p => p.SupplierId);
        builder.HasIndex(p => p.Status);

        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}
