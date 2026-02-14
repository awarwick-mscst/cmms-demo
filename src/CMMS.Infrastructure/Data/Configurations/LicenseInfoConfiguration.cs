using CMMS.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMMS.Infrastructure.Data.Configurations;

public class LicenseInfoConfiguration : IEntityTypeConfiguration<LicenseInfo>
{
    public void Configure(EntityTypeBuilder<LicenseInfo> builder)
    {
        builder.ToTable("license_info", "core");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.LicenseKey)
            .HasColumnName("license_key")
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(l => l.Tier)
            .HasColumnName("tier")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(l => l.Features)
            .HasColumnName("features")
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(l => l.HardwareId)
            .HasColumnName("hardware_id")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(l => l.ActivationId)
            .HasColumnName("activation_id");

        builder.Property(l => l.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired();

        builder.Property(l => l.LastPhoneHome)
            .HasColumnName("last_phone_home");

        builder.Property(l => l.LastPhoneHomeResponse)
            .HasColumnName("last_phone_home_response")
            .HasMaxLength(4000);

        builder.Property(l => l.Status)
            .HasColumnName("status")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(l => l.WarningMessage)
            .HasColumnName("warning_message")
            .HasMaxLength(500);

        builder.Property(l => l.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql(SqlDialect.UtcNow());

        builder.Property(l => l.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(l => l.CreatedBy)
            .HasColumnName("created_by");

        builder.Property(l => l.UpdatedBy)
            .HasColumnName("updated_by");

        builder.Property(l => l.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValue(false);

        builder.Property(l => l.DeletedAt)
            .HasColumnName("deleted_at");

        builder.HasQueryFilter(l => !l.IsDeleted);
    }
}
