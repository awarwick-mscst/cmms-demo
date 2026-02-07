using CMMS.Core.Entities;
using CMMS.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMMS.Infrastructure.Data.Configurations;

public class AssetCategoryConfiguration : IEntityTypeConfiguration<AssetCategory>
{
    public void Configure(EntityTypeBuilder<AssetCategory> builder)
    {
        builder.ToTable("asset_categories", "assets");

        builder.HasKey(ac => ac.Id);
        builder.Property(ac => ac.Id).HasColumnName("id");

        builder.Property(ac => ac.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(ac => ac.Code)
            .HasColumnName("code")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(ac => ac.Description)
            .HasColumnName("description")
            .HasMaxLength(500);

        builder.Property(ac => ac.ParentId)
            .HasColumnName("parent_id");

        builder.Property(ac => ac.Level)
            .HasColumnName("level")
            .HasDefaultValue(0);

        builder.Property(ac => ac.SortOrder)
            .HasColumnName("sort_order")
            .HasDefaultValue(0);

        builder.Property(ac => ac.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(ac => ac.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(ac => ac.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(ac => ac.CreatedBy)
            .HasColumnName("created_by");

        builder.Property(ac => ac.UpdatedBy)
            .HasColumnName("updated_by");

        builder.Property(ac => ac.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValue(false);

        builder.Property(ac => ac.DeletedAt)
            .HasColumnName("deleted_at");

        builder.HasOne(ac => ac.Parent)
            .WithMany(ac => ac.Children)
            .HasForeignKey(ac => ac.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(ac => ac.Code)
            .IsUnique()
            .HasFilter("[is_deleted] = 0");

        builder.HasQueryFilter(ac => !ac.IsDeleted);
    }
}

public class AssetLocationConfiguration : IEntityTypeConfiguration<AssetLocation>
{
    public void Configure(EntityTypeBuilder<AssetLocation> builder)
    {
        builder.ToTable("asset_locations", "assets");

        builder.HasKey(al => al.Id);
        builder.Property(al => al.Id).HasColumnName("id");

        builder.Property(al => al.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(al => al.Code)
            .HasColumnName("code")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(al => al.Description)
            .HasColumnName("description")
            .HasMaxLength(500);

        builder.Property(al => al.ParentId)
            .HasColumnName("parent_id");

        builder.Property(al => al.Level)
            .HasColumnName("level")
            .HasDefaultValue(0);

        builder.Property(al => al.FullPath)
            .HasColumnName("full_path")
            .HasMaxLength(500);

        builder.Property(al => al.Building)
            .HasColumnName("building")
            .HasMaxLength(100);

        builder.Property(al => al.Floor)
            .HasColumnName("floor")
            .HasMaxLength(20);

        builder.Property(al => al.Room)
            .HasColumnName("room")
            .HasMaxLength(50);

        builder.Property(al => al.Latitude)
            .HasColumnName("latitude")
            .HasPrecision(10, 8);

        builder.Property(al => al.Longitude)
            .HasColumnName("longitude")
            .HasPrecision(11, 8);

        builder.Property(al => al.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(al => al.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(al => al.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(al => al.CreatedBy)
            .HasColumnName("created_by");

        builder.Property(al => al.UpdatedBy)
            .HasColumnName("updated_by");

        builder.Property(al => al.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValue(false);

        builder.Property(al => al.DeletedAt)
            .HasColumnName("deleted_at");

        builder.HasOne(al => al.Parent)
            .WithMany(al => al.Children)
            .HasForeignKey(al => al.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(al => al.Code)
            .IsUnique()
            .HasFilter("[is_deleted] = 0");

        builder.HasQueryFilter(al => !al.IsDeleted);
    }
}

public class AssetConfiguration : IEntityTypeConfiguration<Asset>
{
    public void Configure(EntityTypeBuilder<Asset> builder)
    {
        builder.ToTable("assets", "assets");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id");

        builder.Property(a => a.AssetTag)
            .HasColumnName("asset_tag")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(a => a.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(a => a.Description)
            .HasColumnName("description");

        builder.Property(a => a.CategoryId)
            .HasColumnName("category_id")
            .IsRequired();

        builder.Property(a => a.LocationId)
            .HasColumnName("location_id");

        builder.Property(a => a.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasConversion(
                v => v.ToString(),
                v => Enum.Parse<AssetStatus>(v))
            .HasDefaultValue(AssetStatus.Active);

        builder.Property(a => a.Criticality)
            .HasColumnName("criticality")
            .HasMaxLength(20)
            .HasConversion(
                v => v.ToString(),
                v => Enum.Parse<AssetCriticality>(v))
            .HasDefaultValue(AssetCriticality.Medium);

        builder.Property(a => a.Manufacturer)
            .HasColumnName("manufacturer")
            .HasMaxLength(100);

        builder.Property(a => a.Model)
            .HasColumnName("model")
            .HasMaxLength(100);

        builder.Property(a => a.SerialNumber)
            .HasColumnName("serial_number")
            .HasMaxLength(100);

        builder.Property(a => a.Barcode)
            .HasColumnName("barcode")
            .HasMaxLength(100);

        builder.Property(a => a.PurchaseDate)
            .HasColumnName("purchase_date");

        builder.Property(a => a.PurchaseCost)
            .HasColumnName("purchase_cost")
            .HasPrecision(18, 2);

        builder.Property(a => a.WarrantyExpiry)
            .HasColumnName("warranty_expiry");

        builder.Property(a => a.ExpectedLifeYears)
            .HasColumnName("expected_life_years");

        builder.Property(a => a.InstallationDate)
            .HasColumnName("installation_date");

        builder.Property(a => a.LastMaintenanceDate)
            .HasColumnName("last_maintenance_date");

        builder.Property(a => a.NextMaintenanceDate)
            .HasColumnName("next_maintenance_date");

        builder.Property(a => a.ParentAssetId)
            .HasColumnName("parent_asset_id");

        builder.Property(a => a.AssignedTo)
            .HasColumnName("assigned_to");

        builder.Property(a => a.Notes)
            .HasColumnName("notes");

        builder.Property(a => a.CustomFields)
            .HasColumnName("custom_fields");

        builder.Property(a => a.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(a => a.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(a => a.CreatedBy)
            .HasColumnName("created_by");

        builder.Property(a => a.UpdatedBy)
            .HasColumnName("updated_by");

        builder.Property(a => a.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValue(false);

        builder.Property(a => a.DeletedAt)
            .HasColumnName("deleted_at");

        builder.HasOne(a => a.Category)
            .WithMany(c => c.Assets)
            .HasForeignKey(a => a.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Location)
            .WithMany(l => l.Assets)
            .HasForeignKey(a => a.LocationId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(a => a.ParentAsset)
            .WithMany(a => a.ChildAssets)
            .HasForeignKey(a => a.ParentAssetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.AssignedUser)
            .WithMany()
            .HasForeignKey(a => a.AssignedTo)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(a => a.AssetTag)
            .IsUnique()
            .HasFilter("[is_deleted] = 0");

        builder.HasIndex(a => a.Barcode)
            .IsUnique()
            .HasFilter("[is_deleted] = 0 AND [barcode] IS NOT NULL");

        builder.HasQueryFilter(a => !a.IsDeleted);
    }
}

public class AssetDocumentConfiguration : IEntityTypeConfiguration<AssetDocument>
{
    public void Configure(EntityTypeBuilder<AssetDocument> builder)
    {
        builder.ToTable("asset_documents", "assets");

        builder.HasKey(ad => ad.Id);
        builder.Property(ad => ad.Id).HasColumnName("id");

        builder.Property(ad => ad.AssetId)
            .HasColumnName("asset_id")
            .IsRequired();

        builder.Property(ad => ad.DocumentType)
            .HasColumnName("document_type")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(ad => ad.Title)
            .HasColumnName("title")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(ad => ad.FileName)
            .HasColumnName("file_name")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(ad => ad.FilePath)
            .HasColumnName("file_path")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(ad => ad.FileSize)
            .HasColumnName("file_size");

        builder.Property(ad => ad.MimeType)
            .HasColumnName("mime_type")
            .HasMaxLength(100);

        builder.Property(ad => ad.Description)
            .HasColumnName("description")
            .HasMaxLength(500);

        builder.Property(ad => ad.UploadedAt)
            .HasColumnName("uploaded_at")
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(ad => ad.UploadedBy)
            .HasColumnName("uploaded_by");

        builder.Property(ad => ad.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValue(false);

        builder.Property(ad => ad.DeletedAt)
            .HasColumnName("deleted_at");

        builder.HasOne(ad => ad.Asset)
            .WithMany(a => a.Documents)
            .HasForeignKey(ad => ad.AssetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ad => ad.Uploader)
            .WithMany()
            .HasForeignKey(ad => ad.UploadedBy)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasQueryFilter(ad => !ad.IsDeleted);
    }
}
