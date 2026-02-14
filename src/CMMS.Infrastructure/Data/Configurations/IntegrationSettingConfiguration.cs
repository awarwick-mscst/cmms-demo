using CMMS.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMMS.Infrastructure.Data.Configurations;

public class IntegrationSettingConfiguration : IEntityTypeConfiguration<IntegrationSetting>
{
    public void Configure(EntityTypeBuilder<IntegrationSetting> builder)
    {
        builder.ToTable("integration_settings", "core");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id");

        builder.Property(s => s.ProviderType)
            .HasColumnName("provider_type")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(s => s.SettingKey)
            .HasColumnName("setting_key")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(s => s.EncryptedValue)
            .HasColumnName("encrypted_value")
            .IsRequired();

        builder.Property(s => s.ExpiresAt)
            .HasColumnName("expires_at");

        // Audit fields
        builder.Property(s => s.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql(SqlDialect.UtcNow());

        builder.Property(s => s.CreatedBy)
            .HasColumnName("created_by");

        builder.Property(s => s.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(s => s.UpdatedBy)
            .HasColumnName("updated_by");

        builder.Property(s => s.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValue(false);

        builder.Property(s => s.DeletedAt)
            .HasColumnName("deleted_at");

        // Unique constraint
        builder.HasIndex(s => new { s.ProviderType, s.SettingKey })
            .IsUnique();

        // Index
        builder.HasIndex(s => s.ProviderType)
            .HasFilter(SqlDialect.SoftDeleteFilter());

        // Soft delete filter
        builder.HasQueryFilter(s => !s.IsDeleted);
    }
}
