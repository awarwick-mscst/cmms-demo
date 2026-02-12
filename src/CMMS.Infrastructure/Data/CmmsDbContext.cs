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

    // Inventory schema
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<PartCategory> PartCategories => Set<PartCategory>();
    public DbSet<StorageLocation> StorageLocations => Set<StorageLocation>();
    public DbSet<Part> Parts => Set<Part>();
    public DbSet<PartStock> PartStocks => Set<PartStock>();
    public DbSet<PartTransaction> PartTransactions => Set<PartTransaction>();
    public DbSet<AssetPart> AssetParts => Set<AssetPart>();

    // Maintenance schema
    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();
    public DbSet<WorkOrderHistory> WorkOrderHistory => Set<WorkOrderHistory>();
    public DbSet<WorkOrderComment> WorkOrderComments => Set<WorkOrderComment>();
    public DbSet<WorkOrderLabor> WorkOrderLabor => Set<WorkOrderLabor>();
    public DbSet<WorkOrderTask> WorkOrderTasks => Set<WorkOrderTask>();
    public DbSet<WorkOrderTaskTemplate> WorkOrderTaskTemplates => Set<WorkOrderTaskTemplate>();
    public DbSet<WorkOrderTaskTemplateItem> WorkOrderTaskTemplateItems => Set<WorkOrderTaskTemplateItem>();
    public DbSet<PreventiveMaintenanceSchedule> PreventiveMaintenanceSchedules => Set<PreventiveMaintenanceSchedule>();
    public DbSet<WorkSession> WorkSessions => Set<WorkSession>();

    // Admin schema
    public DbSet<LabelTemplate> LabelTemplates => Set<LabelTemplate>();
    public DbSet<LabelPrinter> LabelPrinters => Set<LabelPrinter>();

    // Attachments
    public DbSet<Attachment> Attachments => Set<Attachment>();

    // Licensing
    public DbSet<LicenseInfo> LicenseInfos => Set<LicenseInfo>();

    // Notifications
    public DbSet<NotificationQueue> NotificationQueue => Set<NotificationQueue>();
    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();
    public DbSet<UserNotificationPreference> UserNotificationPreferences => Set<UserNotificationPreference>();
    public DbSet<IntegrationSetting> IntegrationSettings => Set<IntegrationSetting>();
    public DbSet<CalendarEvent> CalendarEvents => Set<CalendarEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CmmsDbContext).Assembly);
    }
}
