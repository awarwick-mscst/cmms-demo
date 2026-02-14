using LicensingServer.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LicensingServer.Web.Data.Configurations;

public class ReleaseConfiguration : IEntityTypeConfiguration<Release>
{
    public void Configure(EntityTypeBuilder<Release> builder)
    {
        builder.ToTable("releases");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Version)
            .HasColumnName("version")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(r => r.ReleaseNotes)
            .HasColumnName("release_notes")
            .HasMaxLength(4000);

        builder.Property(r => r.FileName)
            .HasColumnName("file_name")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(r => r.FileSizeBytes)
            .HasColumnName("file_size_bytes")
            .IsRequired();

        builder.Property(r => r.Sha256Hash)
            .HasColumnName("sha256_hash")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(r => r.IsRequired)
            .HasColumnName("is_required")
            .HasDefaultValue(false);

        builder.Property(r => r.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(false);

        builder.Property(r => r.Channel)
            .HasColumnName("channel")
            .HasMaxLength(50)
            .HasDefaultValue("stable");

        builder.Property(r => r.PublishedAt)
            .HasColumnName("published_at")
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(r => r.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasIndex(r => r.Version).IsUnique();
        builder.HasIndex(r => new { r.Channel, r.IsActive });
    }
}
