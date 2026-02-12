using LicensingServer.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LicensingServer.Web.Data.Configurations;

public class AdminUserConfiguration : IEntityTypeConfiguration<AdminUser>
{
    public void Configure(EntityTypeBuilder<AdminUser> builder)
    {
        builder.ToTable("admin_users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Username)
            .HasColumnName("username")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(u => u.Email)
            .HasColumnName("email")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(u => u.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(u => u.RequireMfa)
            .HasColumnName("require_mfa")
            .HasDefaultValue(true);

        builder.Property(u => u.TotpEnabled)
            .HasColumnName("totp_enabled")
            .HasDefaultValue(false);

        builder.Property(u => u.TotpSecretEncrypted)
            .HasColumnName("totp_secret_encrypted")
            .HasMaxLength(500);

        builder.Property(u => u.RecoveryCodesEncrypted)
            .HasColumnName("recovery_codes_encrypted")
            .HasMaxLength(2000);

        builder.Property(u => u.AccountLocked)
            .HasColumnName("account_locked")
            .HasDefaultValue(false);

        builder.Property(u => u.FailedLoginAttempts)
            .HasColumnName("failed_login_attempts")
            .HasDefaultValue(0);

        builder.Property(u => u.LockedUntil)
            .HasColumnName("locked_until");

        builder.Property(u => u.LastLoginAt)
            .HasColumnName("last_login_at");

        builder.Property(u => u.LastLoginIp)
            .HasColumnName("last_login_ip")
            .HasMaxLength(45);

        builder.Property(u => u.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(u => u.UpdatedAt)
            .HasColumnName("updated_at");

        builder.HasIndex(u => u.Username).IsUnique();
        builder.HasIndex(u => u.Email).IsUnique();
    }
}
