-- =============================================
-- Maintenance Schema Migration Script
-- Creates tables for work order management
-- =============================================

-- Create maintenance schema if not exists
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'maintenance')
BEGIN
    EXEC('CREATE SCHEMA maintenance');
END
GO

-- =============================================
-- Table: maintenance.preventive_maintenance_schedules
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE t.name = 'preventive_maintenance_schedules' AND s.name = 'maintenance')
BEGIN
    CREATE TABLE maintenance.preventive_maintenance_schedules (
        id INT IDENTITY(1,1) PRIMARY KEY,
        name NVARCHAR(200) NOT NULL,
        description NVARCHAR(4000) NULL,
        asset_id INT NULL,
        frequency_type NVARCHAR(20) NOT NULL,
        frequency_value INT NOT NULL DEFAULT 1,
        day_of_week INT NULL,
        day_of_month INT NULL,
        next_due_date DATETIME2 NULL,
        last_completed_date DATETIME2 NULL,
        lead_time_days INT NOT NULL DEFAULT 0,
        work_order_title NVARCHAR(200) NOT NULL,
        work_order_description NVARCHAR(4000) NULL,
        priority NVARCHAR(20) NOT NULL DEFAULT 'Medium',
        estimated_hours DECIMAL(10,2) NULL,
        is_active BIT NOT NULL DEFAULT 1,
        created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        updated_at DATETIME2 NULL,
        created_by INT NULL,
        updated_by INT NULL,
        is_deleted BIT NOT NULL DEFAULT 0,
        deleted_at DATETIME2 NULL,
        CONSTRAINT FK_pm_schedules_asset FOREIGN KEY (asset_id) REFERENCES assets.assets(id) ON DELETE SET NULL
    );

    CREATE INDEX IX_pm_schedules_asset ON maintenance.preventive_maintenance_schedules(asset_id);
    CREATE INDEX IX_pm_schedules_next_due ON maintenance.preventive_maintenance_schedules(next_due_date);
    CREATE INDEX IX_pm_schedules_active ON maintenance.preventive_maintenance_schedules(is_active);
END
GO

-- =============================================
-- Table: maintenance.work_orders
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE t.name = 'work_orders' AND s.name = 'maintenance')
BEGIN
    CREATE TABLE maintenance.work_orders (
        id INT IDENTITY(1,1) PRIMARY KEY,
        work_order_number NVARCHAR(50) NOT NULL,
        type NVARCHAR(30) NOT NULL,
        priority NVARCHAR(20) NOT NULL DEFAULT 'Medium',
        status NVARCHAR(20) NOT NULL DEFAULT 'Draft',
        title NVARCHAR(200) NOT NULL,
        description NVARCHAR(4000) NULL,
        asset_id INT NULL,
        location_id INT NULL,
        requested_by NVARCHAR(200) NULL,
        requested_date DATETIME2 NULL,
        assigned_to_id INT NULL,
        scheduled_start_date DATETIME2 NULL,
        scheduled_end_date DATETIME2 NULL,
        actual_start_date DATETIME2 NULL,
        actual_end_date DATETIME2 NULL,
        estimated_hours DECIMAL(10,2) NULL,
        actual_hours DECIMAL(10,2) NULL,
        completion_notes NVARCHAR(4000) NULL,
        preventive_maintenance_schedule_id INT NULL,
        created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        updated_at DATETIME2 NULL,
        created_by INT NULL,
        updated_by INT NULL,
        is_deleted BIT NOT NULL DEFAULT 0,
        deleted_at DATETIME2 NULL,
        CONSTRAINT FK_work_orders_asset FOREIGN KEY (asset_id) REFERENCES assets.assets(id) ON DELETE SET NULL,
        CONSTRAINT FK_work_orders_location FOREIGN KEY (location_id) REFERENCES assets.asset_locations(id) ON DELETE SET NULL,
        CONSTRAINT FK_work_orders_assigned_to FOREIGN KEY (assigned_to_id) REFERENCES core.users(id) ON DELETE SET NULL,
        CONSTRAINT FK_work_orders_pm_schedule FOREIGN KEY (preventive_maintenance_schedule_id) REFERENCES maintenance.preventive_maintenance_schedules(id) ON DELETE SET NULL
    );

    CREATE UNIQUE INDEX IX_work_orders_number ON maintenance.work_orders(work_order_number) WHERE is_deleted = 0;
    CREATE INDEX IX_work_orders_status ON maintenance.work_orders(status);
    CREATE INDEX IX_work_orders_type ON maintenance.work_orders(type);
    CREATE INDEX IX_work_orders_priority ON maintenance.work_orders(priority);
    CREATE INDEX IX_work_orders_asset ON maintenance.work_orders(asset_id);
    CREATE INDEX IX_work_orders_assigned_to ON maintenance.work_orders(assigned_to_id);
    CREATE INDEX IX_work_orders_scheduled_start ON maintenance.work_orders(scheduled_start_date);
END
GO

-- =============================================
-- Table: maintenance.work_order_history
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE t.name = 'work_order_history' AND s.name = 'maintenance')
BEGIN
    CREATE TABLE maintenance.work_order_history (
        id INT IDENTITY(1,1) PRIMARY KEY,
        work_order_id INT NOT NULL,
        from_status NVARCHAR(20) NULL,
        to_status NVARCHAR(20) NOT NULL,
        changed_by_id INT NOT NULL,
        changed_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        notes NVARCHAR(1000) NULL,
        CONSTRAINT FK_wo_history_work_order FOREIGN KEY (work_order_id) REFERENCES maintenance.work_orders(id) ON DELETE CASCADE,
        CONSTRAINT FK_wo_history_changed_by FOREIGN KEY (changed_by_id) REFERENCES core.users(id)
    );

    CREATE INDEX IX_wo_history_work_order ON maintenance.work_order_history(work_order_id);
    CREATE INDEX IX_wo_history_changed_at ON maintenance.work_order_history(changed_at);
END
GO

-- =============================================
-- Table: maintenance.work_order_comments
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE t.name = 'work_order_comments' AND s.name = 'maintenance')
BEGIN
    CREATE TABLE maintenance.work_order_comments (
        id INT IDENTITY(1,1) PRIMARY KEY,
        work_order_id INT NOT NULL,
        comment NVARCHAR(4000) NOT NULL,
        is_internal BIT NOT NULL DEFAULT 0,
        created_by_id INT NOT NULL,
        created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_wo_comments_work_order FOREIGN KEY (work_order_id) REFERENCES maintenance.work_orders(id) ON DELETE CASCADE,
        CONSTRAINT FK_wo_comments_created_by FOREIGN KEY (created_by_id) REFERENCES core.users(id)
    );

    CREATE INDEX IX_wo_comments_work_order ON maintenance.work_order_comments(work_order_id);
    CREATE INDEX IX_wo_comments_created_at ON maintenance.work_order_comments(created_at);
END
GO

-- =============================================
-- Table: maintenance.work_order_labor
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE t.name = 'work_order_labor' AND s.name = 'maintenance')
BEGIN
    CREATE TABLE maintenance.work_order_labor (
        id INT IDENTITY(1,1) PRIMARY KEY,
        work_order_id INT NOT NULL,
        user_id INT NOT NULL,
        work_date DATETIME2 NOT NULL,
        hours_worked DECIMAL(10,2) NOT NULL,
        labor_type NVARCHAR(20) NOT NULL DEFAULT 'Regular',
        hourly_rate DECIMAL(10,2) NULL,
        notes NVARCHAR(1000) NULL,
        created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_wo_labor_work_order FOREIGN KEY (work_order_id) REFERENCES maintenance.work_orders(id) ON DELETE CASCADE,
        CONSTRAINT FK_wo_labor_user FOREIGN KEY (user_id) REFERENCES core.users(id)
    );

    CREATE INDEX IX_wo_labor_work_order ON maintenance.work_order_labor(work_order_id);
    CREATE INDEX IX_wo_labor_user ON maintenance.work_order_labor(user_id);
    CREATE INDEX IX_wo_labor_work_date ON maintenance.work_order_labor(work_date);
END
GO

-- =============================================
-- Add foreign key from asset_parts to work_orders
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_asset_parts_work_order')
BEGIN
    ALTER TABLE inventory.asset_parts
    ADD CONSTRAINT FK_asset_parts_work_order
    FOREIGN KEY (work_order_id) REFERENCES maintenance.work_orders(id) ON DELETE SET NULL;
END
GO

-- =============================================
-- Add maintenance permissions
-- =============================================
IF NOT EXISTS (SELECT * FROM core.permissions WHERE name = 'work-orders.view')
BEGIN
    INSERT INTO core.permissions (name, description, module, created_at)
    VALUES
        ('work-orders.view', 'View work orders', 'Maintenance', GETUTCDATE()),
        ('work-orders.create', 'Create work orders', 'Maintenance', GETUTCDATE()),
        ('work-orders.edit', 'Edit work orders', 'Maintenance', GETUTCDATE()),
        ('work-orders.delete', 'Delete work orders', 'Maintenance', GETUTCDATE()),
        ('preventive-maintenance.view', 'View preventive maintenance schedules', 'Maintenance', GETUTCDATE()),
        ('preventive-maintenance.manage', 'Manage preventive maintenance schedules', 'Maintenance', GETUTCDATE());
END
GO

-- Add permissions to Administrator role
DECLARE @adminRoleId INT = (SELECT id FROM core.roles WHERE name = 'Administrator');

IF @adminRoleId IS NOT NULL
BEGIN
    INSERT INTO core.role_permissions (role_id, permission_id, created_at)
    SELECT @adminRoleId, id, GETUTCDATE()
    FROM core.permissions
    WHERE name IN ('work-orders.view', 'work-orders.create', 'work-orders.edit', 'work-orders.delete', 'preventive-maintenance.view', 'preventive-maintenance.manage')
    AND id NOT IN (SELECT permission_id FROM core.role_permissions WHERE role_id = @adminRoleId);
END
GO

-- Add permissions to Technician role (if exists)
DECLARE @techRoleId INT = (SELECT id FROM core.roles WHERE name = 'Technician');

IF @techRoleId IS NOT NULL
BEGIN
    INSERT INTO core.role_permissions (role_id, permission_id, created_at)
    SELECT @techRoleId, id, GETUTCDATE()
    FROM core.permissions
    WHERE name IN ('work-orders.view', 'work-orders.create', 'work-orders.edit', 'preventive-maintenance.view')
    AND id NOT IN (SELECT permission_id FROM core.role_permissions WHERE role_id = @techRoleId);
END
GO

PRINT 'Maintenance schema migration completed successfully.';
GO
