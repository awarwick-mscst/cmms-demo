using LicensingServer.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LicensingServer.Web.Data.Configurations;

public class LicenseAuditLogConfiguration : IEntityTypeConfiguration<LicenseAuditLog>
{
    public void Configure(EntityTypeBuilder<LicenseAuditLog> builder)
    {
        builder.ToTable("license_audit_logs");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.LicenseId)
            .HasColumnName("license_id")
            .IsRequired();

        builder.Property(a => a.Action)
            .HasColumnName("action")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.Details)
            .HasColumnName("details")
            .HasMaxLength(2000);

        builder.Property(a => a.IpAddress)
            .HasColumnName("ip_address")
            .HasMaxLength(45);

        builder.Property(a => a.HardwareId)
            .HasColumnName("hardware_id")
            .HasMaxLength(256);

        builder.Property(a => a.Timestamp)
            .HasColumnName("timestamp")
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(a => a.License)
            .WithMany(l => l.AuditLogs)
            .HasForeignKey(a => a.LicenseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(a => a.Timestamp);
    }
}
