using CMMS.Core.Entities;

namespace CMMS.Core.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IRepository<User> Users { get; }
    IRepository<Role> Roles { get; }
    IRepository<Permission> Permissions { get; }
    IRepository<UserRole> UserRoles { get; }
    IRepository<RolePermission> RolePermissions { get; }
    IRepository<RefreshToken> RefreshTokens { get; }
    IRepository<AuditLog> AuditLogs { get; }
    IRepository<Asset> Assets { get; }
    IRepository<AssetCategory> AssetCategories { get; }
    IRepository<AssetLocation> AssetLocations { get; }
    IRepository<AssetDocument> AssetDocuments { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
