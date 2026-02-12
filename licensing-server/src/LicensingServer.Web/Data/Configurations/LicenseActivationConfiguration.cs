using LicensingServer.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LicensingServer.Web.Data.Configurations;

public class LicenseActivationConfiguration : IEntityTypeConfiguration<LicenseActivation>
{
    public void Configure(EntityTypeBuilder<LicenseActivation> builder)
    {
        builder.ToTable("license_activations");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.LicenseId)
            .HasColumnName("license_id")
            .IsRequired();

        builder.Property(a => a.HardwareId)
            .HasColumnName("hardware_id")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(a => a.MachineName)
            .HasColumnName("machine_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(a => a.OsInfo)
            .HasColumnName("os_info")
            .HasMaxLength(500);

        builder.Property(a => a.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(a => a.ActivatedAt)
            .HasColumnName("activated_at")
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(a => a.DeactivatedAt)
            .HasColumnName("deactivated_at");

        builder.Property(a => a.LastPhoneHome)
            .HasColumnName("last_phone_home");

        builder.Property(a => a.LastIpAddress)
            .HasColumnName("last_ip_address")
            .HasMaxLength(45);

        builder.HasOne(a => a.License)
            .WithMany(l => l.Activations)
            .HasForeignKey(a => a.LicenseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(a => new { a.LicenseId, a.HardwareId })
            .IsUnique()
            .HasFilter("[is_active] = 1");
    }
}
