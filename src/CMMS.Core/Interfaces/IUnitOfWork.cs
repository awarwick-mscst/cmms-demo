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
    IRepository<Supplier> Suppliers { get; }
    IRepository<PartCategory> PartCategories { get; }
    IRepository<StorageLocation> StorageLocations { get; }
    IRepository<Part> Parts { get; }
    IRepository<PartStock> PartStocks { get; }
    IRepository<PartTransaction> PartTransactions { get; }
    IRepository<AssetPart> AssetParts { get; }

    // Maintenance
    IRepository<WorkOrder> WorkOrders { get; }
    IRepository<WorkOrderHistory> WorkOrderHistory { get; }
    IRepository<WorkOrderComment> WorkOrderComments { get; }
    IRepository<WorkOrderLabor> WorkOrderLabor { get; }
    IRepository<WorkOrderTask> WorkOrderTasks { get; }
    IRepository<WorkOrderTaskTemplate> WorkOrderTaskTemplates { get; }
    IRepository<WorkOrderTaskTemplateItem> WorkOrderTaskTemplateItems { get; }
    IRepository<PreventiveMaintenanceSchedule> PreventiveMaintenanceSchedules { get; }
    IRepository<WorkSession> WorkSessions { get; }

    // Admin
    IRepository<LabelTemplate> LabelTemplates { get; }
    IRepository<LabelPrinter> LabelPrinters { get; }

    // Attachments
    IRepository<Attachment> Attachments { get; }

    // Notifications
    IRepository<NotificationQueue> NotificationQueue { get; }
    IRepository<NotificationLog> NotificationLogs { get; }
    IRepository<UserNotificationPreference> UserNotificationPreferences { get; }
    IRepository<IntegrationSetting> IntegrationSettings { get; }
    IRepository<CalendarEvent> CalendarEvents { get; }

    // Licensing
    IRepository<LicenseInfo> LicenseInfos { get; }

    // AI Assistant
    IRepository<AiConversation> AiConversations { get; }
    IRepository<AiMessage> AiMessages { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
