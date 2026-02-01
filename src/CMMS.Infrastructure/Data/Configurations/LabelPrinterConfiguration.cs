using CMMS.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMMS.Infrastructure.Data.Configurations;

public class LabelPrinterConfiguration : IEntityTypeConfiguration<LabelPrinter>
{
    public void Configure(EntityTypeBuilder<LabelPrinter> builder)
    {
        builder.ToTable("label_printers", "admin");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id");

        builder.Property(p => p.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(p => p.IpAddress)
            .HasColumnName("ip_address")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.Port)
            .HasColumnName("port")
            .HasDefaultValue(9100);

        builder.Property(p => p.PrinterModel)
            .HasColumnName("printer_model")
            .HasMaxLength(100);

        builder.Property(p => p.Dpi)
            .HasColumnName("dpi")
            .HasDefaultValue(203);

        builder.Property(p => p.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(p => p.IsDefault)
            .HasColumnName("is_default")
            .HasDefaultValue(false);

        builder.Property(p => p.Location)
            .HasColumnName("location")
            .HasMaxLength(200);

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(p => p.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValue(false);

        builder.Property(p => p.DeletedAt)
            .HasColumnName("deleted_at");

        builder.HasIndex(p => p.Name)
            .IsUnique()
            .HasFilter("[is_deleted] = 0");

        builder.HasIndex(p => p.IsActive)
            .HasFilter("[is_deleted] = 0");

        builder.HasIndex(p => p.IsDefault)
            .HasFilter("[is_deleted] = 0");

        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}
