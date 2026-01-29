using CMMS.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace CMMS.Infrastructure.Data;

public class CmmsDbContext : DbContext
{
    public CmmsDbContext(DbContextOptions<CmmsDbContext> options) : base(options)
    {
    }

    // Core schema
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    // Assets schema
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<AssetCategory> AssetCategories => Set<AssetCategory>();
    public DbSet<AssetLocation> AssetLocations => Set<AssetLocation>();
    public DbSet<AssetDocument> AssetDocuments => Set<AssetDocument>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CmmsDbContext).Assembly);
    }
}
