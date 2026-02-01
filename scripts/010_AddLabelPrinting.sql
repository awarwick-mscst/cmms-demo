-- =============================================
-- Label Printing Schema Migration Script
-- Creates tables for label templates and printers
-- =============================================

-- Create admin schema if not exists
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'admin')
BEGIN
    EXEC('CREATE SCHEMA admin');
END
GO

-- =============================================
-- Table: admin.label_templates
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE t.name = 'label_templates' AND s.name = 'admin')
BEGIN
    CREATE TABLE admin.label_templates (
        id INT IDENTITY(1,1) PRIMARY KEY,
        name NVARCHAR(100) NOT NULL,
        description NVARCHAR(500) NULL,
        width DECIMAL(5,2) NOT NULL,           -- in inches
        height DECIMAL(5,2) NOT NULL,          -- in inches
        dpi INT NOT NULL DEFAULT 203,          -- printer DPI (203, 300, 600)
        elements_json NVARCHAR(MAX) NOT NULL,  -- JSON array of elements
        is_default BIT NOT NULL DEFAULT 0,
        created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        updated_at DATETIME2 NULL,
        created_by INT NULL,
        updated_by INT NULL,
        is_deleted BIT NOT NULL DEFAULT 0,
        deleted_at DATETIME2 NULL,
        CONSTRAINT FK_label_templates_created_by FOREIGN KEY (created_by) REFERENCES core.users(id) ON DELETE SET NULL,
        CONSTRAINT FK_label_templates_updated_by FOREIGN KEY (updated_by) REFERENCES core.users(id) ON DELETE NO ACTION
    );

    CREATE UNIQUE INDEX IX_label_templates_name ON admin.label_templates(name) WHERE is_deleted = 0;
    CREATE INDEX IX_label_templates_is_default ON admin.label_templates(is_default) WHERE is_deleted = 0;
END
GO

-- =============================================
-- Table: admin.label_printers
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE t.name = 'label_printers' AND s.name = 'admin')
BEGIN
    CREATE TABLE admin.label_printers (
        id INT IDENTITY(1,1) PRIMARY KEY,
        name NVARCHAR(100) NOT NULL,
        ip_address NVARCHAR(50) NOT NULL,
        port INT NOT NULL DEFAULT 9100,
        printer_model NVARCHAR(100) NULL,      -- e.g., "Zebra ZD420"
        dpi INT NOT NULL DEFAULT 203,
        is_active BIT NOT NULL DEFAULT 1,
        is_default BIT NOT NULL DEFAULT 0,
        location NVARCHAR(200) NULL,           -- physical location description
        created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        updated_at DATETIME2 NULL,
        is_deleted BIT NOT NULL DEFAULT 0,
        deleted_at DATETIME2 NULL
    );

    CREATE UNIQUE INDEX IX_label_printers_name ON admin.label_printers(name) WHERE is_deleted = 0;
    CREATE INDEX IX_label_printers_is_active ON admin.label_printers(is_active) WHERE is_deleted = 0;
    CREATE INDEX IX_label_printers_is_default ON admin.label_printers(is_default) WHERE is_deleted = 0;
END
GO

-- =============================================
-- Add label printing permission
-- =============================================
IF NOT EXISTS (SELECT * FROM core.permissions WHERE name = 'labels.print')
BEGIN
    INSERT INTO core.permissions (name, description, module, created_at)
    VALUES ('labels.print', 'Print labels for parts and assets', 'Labels', GETUTCDATE());
END
GO

IF NOT EXISTS (SELECT * FROM core.permissions WHERE name = 'labels.manage')
BEGIN
    INSERT INTO core.permissions (name, description, module, created_at)
    VALUES ('labels.manage', 'Manage label templates and printers', 'Labels', GETUTCDATE());
END
GO

-- Add permissions to Administrator role
DECLARE @printPermissionId INT = (SELECT id FROM core.permissions WHERE name = 'labels.print');
DECLARE @managePermissionId INT = (SELECT id FROM core.permissions WHERE name = 'labels.manage');
DECLARE @adminRoleId INT = (SELECT id FROM core.roles WHERE name = 'Administrator');

IF @printPermissionId IS NOT NULL AND @adminRoleId IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT * FROM core.role_permissions WHERE role_id = @adminRoleId AND permission_id = @printPermissionId)
    BEGIN
        INSERT INTO core.role_permissions (role_id, permission_id, created_at)
        VALUES (@adminRoleId, @printPermissionId, GETUTCDATE());
    END
END

IF @managePermissionId IS NOT NULL AND @adminRoleId IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT * FROM core.role_permissions WHERE role_id = @adminRoleId AND permission_id = @managePermissionId)
    BEGIN
        INSERT INTO core.role_permissions (role_id, permission_id, created_at)
        VALUES (@adminRoleId, @managePermissionId, GETUTCDATE());
    END
END
GO

-- Add labels.print permission to Inventory Manager role if it exists
DECLARE @invManagerRoleId INT = (SELECT id FROM core.roles WHERE name = 'Inventory Manager');
DECLARE @labelPrintPermId INT = (SELECT id FROM core.permissions WHERE name = 'labels.print');

IF @invManagerRoleId IS NOT NULL AND @labelPrintPermId IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT * FROM core.role_permissions WHERE role_id = @invManagerRoleId AND permission_id = @labelPrintPermId)
    BEGIN
        INSERT INTO core.role_permissions (role_id, permission_id, created_at)
        VALUES (@invManagerRoleId, @labelPrintPermId, GETUTCDATE());
    END
END
GO

-- =============================================
-- Insert default label template
-- =============================================
IF NOT EXISTS (SELECT * FROM admin.label_templates WHERE name = 'Standard Part Label')
BEGIN
    INSERT INTO admin.label_templates (name, description, width, height, dpi, elements_json, is_default, created_at)
    VALUES (
        'Standard Part Label',
        'Default 2x1 inch label with description, part number, and barcode',
        2.0,
        1.0,
        203,
        '[{"type":"text","field":"description","x":10,"y":10,"fontSize":25,"maxWidth":180},{"type":"text","field":"partNumber","x":10,"y":40,"fontSize":20},{"type":"barcode","field":"partNumber","x":10,"y":65,"height":50,"format":"code128"}]',
        1,
        GETUTCDATE()
    );
END
GO

PRINT 'Label printing schema migration completed successfully.';
GO
