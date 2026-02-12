using LicensingServer.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LicensingServer.Web.Data.Configurations;

public class Fido2CredentialConfiguration : IEntityTypeConfiguration<Fido2Credential>
{
    public void Configure(EntityTypeBuilder<Fido2Credential> builder)
    {
        builder.ToTable("fido2_credentials");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.AdminUserId)
            .HasColumnName("admin_user_id")
            .IsRequired();

        builder.Property(c => c.CredentialId)
            .HasColumnName("credential_id")
            .HasMaxLength(1024)
            .IsRequired();

        builder.Property(c => c.PublicKey)
            .HasColumnName("public_key")
            .IsRequired();

        builder.Property(c => c.SignatureCounter)
            .HasColumnName("signature_counter")
            .HasDefaultValue(0L);

        builder.Property(c => c.AaGuid)
            .HasColumnName("aaguid");

        builder.Property(c => c.DeviceName)
            .HasColumnName("device_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.CredentialType)
            .HasColumnName("credential_type")
            .HasMaxLength(50)
            .HasDefaultValue("public-key");

        builder.Property(c => c.Transports)
            .HasColumnName("transports")
            .HasMaxLength(200);

        builder.Property(c => c.IsBackupEligible)
            .HasColumnName("is_backup_eligible")
            .HasDefaultValue(false);

        builder.Property(c => c.IsBackupDevice)
            .HasColumnName("is_backup_device")
            .HasDefaultValue(false);

        builder.Property(c => c.LastUsedAt)
            .HasColumnName("last_used_at");

        builder.Property(c => c.RegisteredAt)
            .HasColumnName("registered_at")
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(c => c.RevokedAt)
            .HasColumnName("revoked_at");

        builder.HasIndex(c => c.CredentialId).IsUnique();
        builder.HasIndex(c => c.AdminUserId);

        builder.HasOne(c => c.AdminUser)
            .WithMany(u => u.Fido2Credentials)
            .HasForeignKey(c => c.AdminUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
