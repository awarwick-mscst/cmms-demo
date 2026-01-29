using CMMS.Core.Entities;
using CMMS.Core.Interfaces;
using CMMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace CMMS.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly CmmsDbContext _context;
    private IDbContextTransaction? _transaction;

    private IRepository<User>? _users;
    private IRepository<Role>? _roles;
    private IRepository<Permission>? _permissions;
    private IRepository<UserRole>? _userRoles;
    private IRepository<RolePermission>? _rolePermissions;
    private IRepository<RefreshToken>? _refreshTokens;
    private IRepository<AuditLog>? _auditLogs;
    private IRepository<Asset>? _assets;
    private IRepository<AssetCategory>? _assetCategories;
    private IRepository<AssetLocation>? _assetLocations;
    private IRepository<AssetDocument>? _assetDocuments;

    public UnitOfWork(CmmsDbContext context)
    {
        _context = context;
    }

    public IRepository<User> Users => _users ??= new Repository<User>(_context);
    public IRepository<Role> Roles => _roles ??= new Repository<Role>(_context);
    public IRepository<Permission> Permissions => _permissions ??= new Repository<Permission>(_context);
    public IRepository<UserRole> UserRoles => _userRoles ??= new Repository<UserRole>(_context);
    public IRepository<RolePermission> RolePermissions => _rolePermissions ??= new Repository<RolePermission>(_context);
    public IRepository<RefreshToken> RefreshTokens => _refreshTokens ??= new Repository<RefreshToken>(_context);
    public IRepository<AuditLog> AuditLogs => _auditLogs ??= new Repository<AuditLog>(_context);
    public IRepository<Asset> Assets => _assets ??= new Repository<Asset>(_context);
    public IRepository<AssetCategory> AssetCategories => _assetCategories ??= new Repository<AssetCategory>(_context);
    public IRepository<AssetLocation> AssetLocations => _assetLocations ??= new Repository<AssetLocation>(_context);
    public IRepository<AssetDocument> AssetDocuments => _assetDocuments ??= new Repository<AssetDocument>(_context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
