using LicensingServer.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LicensingServer.Web.Data.Configurations;

public class LicenseConfiguration : IEntityTypeConfiguration<License>
{
    public void Configure(EntityTypeBuilder<License> builder)
    {
        builder.ToTable("licenses");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.CustomerId)
            .HasColumnName("customer_id")
            .IsRequired();

        builder.Property(l => l.LicenseKey)
            .HasColumnName("license_key")
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(l => l.Tier)
            .HasColumnName("tier")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(l => l.MaxActivations)
            .HasColumnName("max_activations")
            .HasDefaultValue(1);

        builder.Property(l => l.IssuedAt)
            .HasColumnName("issued_at")
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(l => l.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired();

        builder.Property(l => l.IsRevoked)
            .HasColumnName("is_revoked")
            .HasDefaultValue(false);

        builder.Property(l => l.RevokedAt)
            .HasColumnName("revoked_at");

        builder.Property(l => l.RevokedReason)
            .HasColumnName("revoked_reason")
            .HasMaxLength(500);

        builder.Property(l => l.Notes)
            .HasColumnName("notes")
            .HasMaxLength(2000);

        builder.Property(l => l.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(l => l.UpdatedAt)
            .HasColumnName("updated_at");

        builder.HasOne(l => l.Customer)
            .WithMany(c => c.Licenses)
            .HasForeignKey(l => l.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(l => l.LicenseKey).IsUnique();
    }
}
