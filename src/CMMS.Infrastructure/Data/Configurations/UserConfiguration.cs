using CMMS.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMMS.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users", "core");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasColumnName("id");

        builder.Property(u => u.Username)
            .HasColumnName("username")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(u => u.Email)
            .HasColumnName("email")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(u => u.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(u => u.FirstName)
            .HasColumnName("first_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(u => u.LastName)
            .HasColumnName("last_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(u => u.Phone)
            .HasColumnName("phone")
            .HasMaxLength(20);

        builder.Property(u => u.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(u => u.IsLocked)
            .HasColumnName("is_locked")
            .HasDefaultValue(false);

        builder.Property(u => u.FailedLoginAttempts)
            .HasColumnName("failed_login_attempts")
            .HasDefaultValue(0);

        builder.Property(u => u.LockoutEnd)
            .HasColumnName("lockout_end");

        builder.Property(u => u.LastLoginAt)
            .HasColumnName("last_login_at");

        builder.Property(u => u.PasswordChangedAt)
            .HasColumnName("password_changed_at");

        // LDAP/Active Directory properties
        builder.Property(u => u.IsLdapUser)
            .HasColumnName("is_ldap_user")
            .HasDefaultValue(false);

        builder.Property(u => u.LdapDistinguishedName)
            .HasColumnName("ldap_distinguished_name")
            .HasMaxLength(500);

        builder.Property(u => u.LdapLastSyncAt)
            .HasColumnName("ldap_last_sync_at");

        builder.Property(u => u.AuthenticationType)
            .HasColumnName("authentication_type")
            .HasDefaultValue(CMMS.Core.Enums.AuthenticationType.Local);

        builder.Property(u => u.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql(SqlDialect.UtcNow());

        builder.Property(u => u.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(u => u.CreatedBy)
            .HasColumnName("created_by");

        builder.Property(u => u.UpdatedBy)
            .HasColumnName("updated_by");

        builder.Property(u => u.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValue(false);

        builder.Property(u => u.DeletedAt)
            .HasColumnName("deleted_at");

        builder.HasIndex(u => u.Username)
            .IsUnique()
            .HasFilter(SqlDialect.SoftDeleteFilter());

        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasFilter(SqlDialect.SoftDeleteFilter());

        builder.Ignore(u => u.FullName);

        builder.HasQueryFilter(u => !u.IsDeleted);
    }
}
