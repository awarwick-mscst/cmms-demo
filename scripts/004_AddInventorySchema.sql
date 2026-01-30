-- =============================================
-- Inventory Schema Migration Script
-- Creates tables for parts inventory management
-- =============================================

-- Create inventory schema if not exists
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'inventory')
BEGIN
    EXEC('CREATE SCHEMA inventory');
END
GO

-- =============================================
-- Table: inventory.suppliers
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE t.name = 'suppliers' AND s.name = 'inventory')
BEGIN
    CREATE TABLE inventory.suppliers (
        id INT IDENTITY(1,1) PRIMARY KEY,
        name NVARCHAR(200) NOT NULL,
        code NVARCHAR(50) NULL,
        contact_name NVARCHAR(100) NULL,
        email NVARCHAR(255) NULL,
        phone NVARCHAR(50) NULL,
        address NVARCHAR(500) NULL,
        city NVARCHAR(100) NULL,
        state NVARCHAR(100) NULL,
        postal_code NVARCHAR(20) NULL,
        country NVARCHAR(100) NULL,
        website NVARCHAR(500) NULL,
        notes NVARCHAR(2000) NULL,
        is_active BIT NOT NULL DEFAULT 1,
        created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        updated_at DATETIME2 NULL,
        created_by INT NULL,
        updated_by INT NULL,
        is_deleted BIT NOT NULL DEFAULT 0,
        deleted_at DATETIME2 NULL
    );

    CREATE UNIQUE INDEX IX_suppliers_code ON inventory.suppliers(code) WHERE is_deleted = 0 AND code IS NOT NULL;
    CREATE INDEX IX_suppliers_name ON inventory.suppliers(name) WHERE is_deleted = 0;
END
GO

-- =============================================
-- Table: inventory.part_categories
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE t.name = 'part_categories' AND s.name = 'inventory')
BEGIN
    CREATE TABLE inventory.part_categories (
        id INT IDENTITY(1,1) PRIMARY KEY,
        name NVARCHAR(100) NOT NULL,
        code NVARCHAR(50) NULL,
        description NVARCHAR(500) NULL,
        parent_id INT NULL,
        level INT NOT NULL DEFAULT 0,
        sort_order INT NOT NULL DEFAULT 0,
        is_active BIT NOT NULL DEFAULT 1,
        created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        updated_at DATETIME2 NULL,
        created_by INT NULL,
        updated_by INT NULL,
        is_deleted BIT NOT NULL DEFAULT 0,
        deleted_at DATETIME2 NULL,
        CONSTRAINT FK_part_categories_parent FOREIGN KEY (parent_id) REFERENCES inventory.part_categories(id)
    );

    CREATE UNIQUE INDEX IX_part_categories_code ON inventory.part_categories(code) WHERE is_deleted = 0 AND code IS NOT NULL;
    CREATE INDEX IX_part_categories_parent_sort ON inventory.part_categories(parent_id, sort_order);
END
GO

-- =============================================
-- Table: inventory.storage_locations
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE t.name = 'storage_locations' AND s.name = 'inventory')
BEGIN
    CREATE TABLE inventory.storage_locations (
        id INT IDENTITY(1,1) PRIMARY KEY,
        name NVARCHAR(100) NOT NULL,
        code NVARCHAR(50) NULL,
        description NVARCHAR(500) NULL,
        parent_id INT NULL,
        level INT NOT NULL DEFAULT 0,
        full_path NVARCHAR(500) NULL,
        building NVARCHAR(100) NULL,
        aisle NVARCHAR(50) NULL,
        rack NVARCHAR(50) NULL,
        shelf NVARCHAR(50) NULL,
        bin NVARCHAR(50) NULL,
        is_active BIT NOT NULL DEFAULT 1,
        created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        updated_at DATETIME2 NULL,
        created_by INT NULL,
        updated_by INT NULL,
        is_deleted BIT NOT NULL DEFAULT 0,
        deleted_at DATETIME2 NULL,
        CONSTRAINT FK_storage_locations_parent FOREIGN KEY (parent_id) REFERENCES inventory.storage_locations(id)
    );

    CREATE UNIQUE INDEX IX_storage_locations_code ON inventory.storage_locations(code) WHERE is_deleted = 0 AND code IS NOT NULL;
    CREATE INDEX IX_storage_locations_parent ON inventory.storage_locations(parent_id);
END
GO

-- =============================================
-- Table: inventory.parts
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE t.name = 'parts' AND s.name = 'inventory')
BEGIN
    CREATE TABLE inventory.parts (
        id INT IDENTITY(1,1) PRIMARY KEY,
        part_number NVARCHAR(100) NOT NULL,
        name NVARCHAR(200) NOT NULL,
        description NVARCHAR(1000) NULL,
        category_id INT NULL,
        supplier_id INT NULL,
        unit_of_measure NVARCHAR(20) NOT NULL DEFAULT 'Each',
        unit_cost DECIMAL(18,2) NOT NULL DEFAULT 0,
        reorder_point INT NOT NULL DEFAULT 0,
        reorder_quantity INT NOT NULL DEFAULT 0,
        status NVARCHAR(20) NOT NULL DEFAULT 'Active',
        min_stock_level INT NOT NULL DEFAULT 0,
        max_stock_level INT NOT NULL DEFAULT 0,
        lead_time_days INT NOT NULL DEFAULT 0,
        specifications NVARCHAR(MAX) NULL,
        manufacturer NVARCHAR(200) NULL,
        manufacturer_part_number NVARCHAR(100) NULL,
        barcode NVARCHAR(100) NULL,
        image_url NVARCHAR(500) NULL,
        notes NVARCHAR(2000) NULL,
        created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        updated_at DATETIME2 NULL,
        created_by INT NULL,
        updated_by INT NULL,
        is_deleted BIT NOT NULL DEFAULT 0,
        deleted_at DATETIME2 NULL,
        CONSTRAINT FK_parts_category FOREIGN KEY (category_id) REFERENCES inventory.part_categories(id),
        CONSTRAINT FK_parts_supplier FOREIGN KEY (supplier_id) REFERENCES inventory.suppliers(id) ON DELETE SET NULL
    );

    CREATE UNIQUE INDEX IX_parts_part_number ON inventory.parts(part_number) WHERE is_deleted = 0;
    CREATE UNIQUE INDEX IX_parts_barcode ON inventory.parts(barcode) WHERE is_deleted = 0 AND barcode IS NOT NULL;
    CREATE INDEX IX_parts_category ON inventory.parts(category_id);
    CREATE INDEX IX_parts_supplier ON inventory.parts(supplier_id);
    CREATE INDEX IX_parts_status ON inventory.parts(status);
END
GO

-- =============================================
-- Table: inventory.part_stock
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE t.name = 'part_stock' AND s.name = 'inventory')
BEGIN
    CREATE TABLE inventory.part_stock (
        id INT IDENTITY(1,1) PRIMARY KEY,
        part_id INT NOT NULL,
        location_id INT NOT NULL,
        quantity_on_hand INT NOT NULL DEFAULT 0,
        quantity_reserved INT NOT NULL DEFAULT 0,
        last_count_date DATETIME2 NULL,
        last_count_by INT NULL,
        bin_number NVARCHAR(50) NULL,
        shelf_location NVARCHAR(100) NULL,
        created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        updated_at DATETIME2 NULL,
        created_by INT NULL,
        updated_by INT NULL,
        is_deleted BIT NOT NULL DEFAULT 0,
        deleted_at DATETIME2 NULL,
        CONSTRAINT FK_part_stock_part FOREIGN KEY (part_id) REFERENCES inventory.parts(id) ON DELETE CASCADE,
        CONSTRAINT FK_part_stock_location FOREIGN KEY (location_id) REFERENCES inventory.storage_locations(id),
        CONSTRAINT FK_part_stock_count_by FOREIGN KEY (last_count_by) REFERENCES core.users(id) ON DELETE SET NULL
    );

    CREATE UNIQUE INDEX IX_part_stock_part_location ON inventory.part_stock(part_id, location_id) WHERE is_deleted = 0;
    CREATE INDEX IX_part_stock_location ON inventory.part_stock(location_id);
END
GO

-- =============================================
-- Table: inventory.part_transactions
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE t.name = 'part_transactions' AND s.name = 'inventory')
BEGIN
    CREATE TABLE inventory.part_transactions (
        id INT IDENTITY(1,1) PRIMARY KEY,
        part_id INT NOT NULL,
        location_id INT NULL,
        to_location_id INT NULL,
        transaction_type NVARCHAR(20) NOT NULL,
        quantity INT NOT NULL,
        unit_cost DECIMAL(18,2) NOT NULL DEFAULT 0,
        reference_type NVARCHAR(50) NULL,
        reference_id INT NULL,
        notes NVARCHAR(1000) NULL,
        transaction_date DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        created_by INT NULL,
        created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_part_transactions_part FOREIGN KEY (part_id) REFERENCES inventory.parts(id) ON DELETE CASCADE,
        CONSTRAINT FK_part_transactions_location FOREIGN KEY (location_id) REFERENCES inventory.storage_locations(id),
        CONSTRAINT FK_part_transactions_to_location FOREIGN KEY (to_location_id) REFERENCES inventory.storage_locations(id),
        CONSTRAINT FK_part_transactions_created_by FOREIGN KEY (created_by) REFERENCES core.users(id) ON DELETE SET NULL
    );

    CREATE INDEX IX_part_transactions_part ON inventory.part_transactions(part_id);
    CREATE INDEX IX_part_transactions_location ON inventory.part_transactions(location_id);
    CREATE INDEX IX_part_transactions_date ON inventory.part_transactions(transaction_date);
    CREATE INDEX IX_part_transactions_reference ON inventory.part_transactions(reference_type, reference_id);
END
GO

-- =============================================
-- Table: inventory.asset_parts
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE t.name = 'asset_parts' AND s.name = 'inventory')
BEGIN
    CREATE TABLE inventory.asset_parts (
        id INT IDENTITY(1,1) PRIMARY KEY,
        asset_id INT NOT NULL,
        part_id INT NOT NULL,
        quantity_used INT NOT NULL,
        unit_cost_at_time DECIMAL(18,2) NOT NULL DEFAULT 0,
        used_date DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        used_by INT NULL,
        work_order_id INT NULL,
        notes NVARCHAR(1000) NULL,
        created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_asset_parts_asset FOREIGN KEY (asset_id) REFERENCES assets.assets(id) ON DELETE CASCADE,
        CONSTRAINT FK_asset_parts_part FOREIGN KEY (part_id) REFERENCES inventory.parts(id),
        CONSTRAINT FK_asset_parts_used_by FOREIGN KEY (used_by) REFERENCES core.users(id) ON DELETE SET NULL
    );

    CREATE INDEX IX_asset_parts_asset ON inventory.asset_parts(asset_id);
    CREATE INDEX IX_asset_parts_part ON inventory.asset_parts(part_id);
    CREATE INDEX IX_asset_parts_work_order ON inventory.asset_parts(work_order_id);
    CREATE INDEX IX_asset_parts_used_date ON inventory.asset_parts(used_date);
END
GO

-- =============================================
-- Add inventory permission
-- =============================================
IF NOT EXISTS (SELECT * FROM core.permissions WHERE name = 'inventory.manage')
BEGIN
    INSERT INTO core.permissions (name, description, module, created_at)
    VALUES ('inventory.manage', 'Manage inventory including parts, suppliers, and stock', 'Inventory', GETUTCDATE());
END
GO

-- Add permission to Administrator role
DECLARE @permissionId INT = (SELECT id FROM core.permissions WHERE name = 'inventory.manage');
DECLARE @adminRoleId INT = (SELECT id FROM core.roles WHERE name = 'Administrator');

IF @permissionId IS NOT NULL AND @adminRoleId IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT * FROM core.role_permissions WHERE role_id = @adminRoleId AND permission_id = @permissionId)
    BEGIN
        INSERT INTO core.role_permissions (role_id, permission_id, created_at)
        VALUES (@adminRoleId, @permissionId, GETUTCDATE());
    END
END
GO

PRINT 'Inventory schema migration completed successfully.';
GO
