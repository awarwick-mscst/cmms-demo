using CMMS.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMMS.Infrastructure.Data.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens", "core");

        builder.HasKey(rt => rt.Id);
        builder.Property(rt => rt.Id).HasColumnName("id");

        builder.Property(rt => rt.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(rt => rt.Token)
            .HasColumnName("token")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(rt => rt.TokenHash)
            .HasColumnName("token_hash")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(rt => rt.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired();

        builder.Property(rt => rt.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql(SqlDialect.UtcNow());

        builder.Property(rt => rt.CreatedByIp)
            .HasColumnName("created_by_ip")
            .HasMaxLength(45);

        builder.Property(rt => rt.RevokedAt)
            .HasColumnName("revoked_at");

        builder.Property(rt => rt.RevokedByIp)
            .HasColumnName("revoked_by_ip")
            .HasMaxLength(45);

        builder.Property(rt => rt.ReplacedByToken)
            .HasColumnName("replaced_by_token")
            .HasMaxLength(500);

        builder.HasOne(rt => rt.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(rt => rt.TokenHash);
        builder.HasIndex(rt => rt.UserId);

        builder.Ignore(rt => rt.IsActive);
        builder.Ignore(rt => rt.IsExpired);
        builder.Ignore(rt => rt.IsRevoked);
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs", "core");

        builder.HasKey(al => al.Id);
        builder.Property(al => al.Id).HasColumnName("id");

        builder.Property(al => al.UserId)
            .HasColumnName("user_id");

        builder.Property(al => al.Action)
            .HasColumnName("action")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(al => al.EntityType)
            .HasColumnName("entity_type")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(al => al.EntityId)
            .HasColumnName("entity_id")
            .HasMaxLength(50);

        builder.Property(al => al.OldValues)
            .HasColumnName("old_values");

        builder.Property(al => al.NewValues)
            .HasColumnName("new_values");

        builder.Property(al => al.IpAddress)
            .HasColumnName("ip_address")
            .HasMaxLength(45);

        builder.Property(al => al.UserAgent)
            .HasColumnName("user_agent")
            .HasMaxLength(500);

        builder.Property(al => al.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql(SqlDialect.UtcNow());

        builder.HasOne(al => al.User)
            .WithMany()
            .HasForeignKey(al => al.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(al => al.UserId);
        builder.HasIndex(al => new { al.EntityType, al.EntityId });
        builder.HasIndex(al => al.CreatedAt);
    }
}
