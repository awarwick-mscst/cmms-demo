using CMMS.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMMS.Infrastructure.Data.Configurations;

public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("suppliers", "inventory");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id");

        builder.Property(s => s.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(s => s.Code)
            .HasColumnName("code")
            .HasMaxLength(50);

        builder.Property(s => s.ContactName)
            .HasColumnName("contact_name")
            .HasMaxLength(100);

        builder.Property(s => s.Email)
            .HasColumnName("email")
            .HasMaxLength(255);

        builder.Property(s => s.Phone)
            .HasColumnName("phone")
            .HasMaxLength(50);

        builder.Property(s => s.Address)
            .HasColumnName("address")
            .HasMaxLength(500);

        builder.Property(s => s.City)
            .HasColumnName("city")
            .HasMaxLength(100);

        builder.Property(s => s.State)
            .HasColumnName("state")
            .HasMaxLength(100);

        builder.Property(s => s.PostalCode)
            .HasColumnName("postal_code")
            .HasMaxLength(20);

        builder.Property(s => s.Country)
            .HasColumnName("country")
            .HasMaxLength(100);

        builder.Property(s => s.Website)
            .HasColumnName("website")
            .HasMaxLength(500);

        builder.Property(s => s.Notes)
            .HasColumnName("notes")
            .HasMaxLength(2000);

        builder.Property(s => s.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(s => s.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(s => s.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(s => s.CreatedBy)
            .HasColumnName("created_by");

        builder.Property(s => s.UpdatedBy)
            .HasColumnName("updated_by");

        builder.Property(s => s.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValue(false);

        builder.Property(s => s.DeletedAt)
            .HasColumnName("deleted_at");

        builder.HasIndex(s => s.Code)
            .IsUnique()
            .HasFilter("[is_deleted] = 0 AND [code] IS NOT NULL");

        builder.HasIndex(s => s.Name)
            .HasFilter("[is_deleted] = 0");

        builder.HasQueryFilter(s => !s.IsDeleted);
    }
}
