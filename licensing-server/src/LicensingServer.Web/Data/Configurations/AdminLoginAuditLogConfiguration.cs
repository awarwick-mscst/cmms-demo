using LicensingServer.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LicensingServer.Web.Data.Configurations;

public class AdminLoginAuditLogConfiguration : IEntityTypeConfiguration<AdminLoginAuditLog>
{
    public void Configure(EntityTypeBuilder<AdminLoginAuditLog> builder)
    {
        builder.ToTable("admin_login_audit_logs");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.AdminUserId)
            .HasColumnName("admin_user_id");

        builder.Property(l => l.Username)
            .HasColumnName("username")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(l => l.Success)
            .HasColumnName("success")
            .IsRequired();

        builder.Property(l => l.AuthMethod)
            .HasColumnName("auth_method")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(l => l.FailureReason)
            .HasColumnName("failure_reason")
            .HasMaxLength(200);

        builder.Property(l => l.IpAddress)
            .HasColumnName("ip_address")
            .HasMaxLength(45)
            .IsRequired();

        builder.Property(l => l.UserAgent)
            .HasColumnName("user_agent")
            .HasMaxLength(500);

        builder.Property(l => l.Timestamp)
            .HasColumnName("timestamp")
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasIndex(l => l.Timestamp);
        builder.HasIndex(l => l.AdminUserId);
        builder.HasIndex(l => l.Success);

        builder.HasOne(l => l.AdminUser)
            .WithMany(u => u.AuditLogs)
            .HasForeignKey(l => l.AdminUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
