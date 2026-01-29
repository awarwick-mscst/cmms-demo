-- CMMS Seed Data Script
-- SQL Server 2022
-- Run this after 001_InitialSchema.sql

USE CMMS;
GO

-- =============================================
-- SEED ROLES
-- =============================================

SET IDENTITY_INSERT core.roles ON;

INSERT INTO core.roles (id, name, description, is_system_role, created_at)
VALUES
    (1, 'Administrator', 'Full system access with all permissions', 1, GETUTCDATE()),
    (2, 'Manager', 'Manage assets, work orders, and view reports', 1, GETUTCDATE()),
    (3, 'Technician', 'Execute work orders and update asset status', 1, GETUTCDATE()),
    (4, 'Viewer', 'Read-only access to assets and work orders', 1, GETUTCDATE());

SET IDENTITY_INSERT core.roles OFF;
GO

-- =============================================
-- SEED PERMISSIONS
-- =============================================

SET IDENTITY_INSERT core.permissions ON;

INSERT INTO core.permissions (id, name, description, module, created_at)
VALUES
    -- User Management
    (1, 'users.view', 'View users', 'Users', GETUTCDATE()),
    (2, 'users.create', 'Create users', 'Users', GETUTCDATE()),
    (3, 'users.edit', 'Edit users', 'Users', GETUTCDATE()),
    (4, 'users.delete', 'Delete users', 'Users', GETUTCDATE()),

    -- Role Management
    (5, 'roles.view', 'View roles', 'Roles', GETUTCDATE()),
    (6, 'roles.create', 'Create roles', 'Roles', GETUTCDATE()),
    (7, 'roles.edit', 'Edit roles', 'Roles', GETUTCDATE()),
    (8, 'roles.delete', 'Delete roles', 'Roles', GETUTCDATE()),

    -- Asset Management
    (9, 'assets.view', 'View assets', 'Assets', GETUTCDATE()),
    (10, 'assets.create', 'Create assets', 'Assets', GETUTCDATE()),
    (11, 'assets.edit', 'Edit assets', 'Assets', GETUTCDATE()),
    (12, 'assets.delete', 'Delete assets', 'Assets', GETUTCDATE()),

    -- Asset Categories
    (13, 'asset-categories.view', 'View asset categories', 'AssetCategories', GETUTCDATE()),
    (14, 'asset-categories.create', 'Create asset categories', 'AssetCategories', GETUTCDATE()),
    (15, 'asset-categories.edit', 'Edit asset categories', 'AssetCategories', GETUTCDATE()),
    (16, 'asset-categories.delete', 'Delete asset categories', 'AssetCategories', GETUTCDATE()),

    -- Asset Locations
    (17, 'asset-locations.view', 'View asset locations', 'AssetLocations', GETUTCDATE()),
    (18, 'asset-locations.create', 'Create asset locations', 'AssetLocations', GETUTCDATE()),
    (19, 'asset-locations.edit', 'Edit asset locations', 'AssetLocations', GETUTCDATE()),
    (20, 'asset-locations.delete', 'Delete asset locations', 'AssetLocations', GETUTCDATE()),

    -- Reports
    (21, 'reports.view', 'View reports', 'Reports', GETUTCDATE()),
    (22, 'reports.export', 'Export reports', 'Reports', GETUTCDATE()),

    -- Audit
    (23, 'audit.view', 'View audit logs', 'Audit', GETUTCDATE());

SET IDENTITY_INSERT core.permissions OFF;
GO

-- =============================================
-- ASSIGN PERMISSIONS TO ROLES
-- =============================================

-- Administrator - All permissions
INSERT INTO core.role_permissions (role_id, permission_id)
SELECT 1, id FROM core.permissions;

-- Manager - All except user/role management
INSERT INTO core.role_permissions (role_id, permission_id)
SELECT 2, id FROM core.permissions WHERE module NOT IN ('Users', 'Roles');

-- Technician - View and edit assets
INSERT INTO core.role_permissions (role_id, permission_id)
VALUES
    (3, 9),  -- assets.view
    (3, 11), -- assets.edit
    (3, 13), -- asset-categories.view
    (3, 17); -- asset-locations.view

-- Viewer - View only
INSERT INTO core.role_permissions (role_id, permission_id)
VALUES
    (4, 9),  -- assets.view
    (4, 13), -- asset-categories.view
    (4, 17); -- asset-locations.view

GO

-- =============================================
-- SEED DEFAULT ADMIN USER
-- Password: Admin@123 (BCrypt hash)
-- =============================================

SET IDENTITY_INSERT core.users ON;

INSERT INTO core.users (id, username, email, password_hash, first_name, last_name, is_active, created_at, password_changed_at)
VALUES (
    1,
    'admin',
    'admin@cmms.local',
    '$2a$11$K3M8N2Q5R7T9V1X3Z5B7dOgH9K1M3N5P7R9T1V3X5Z7B9D1F3H5J7', -- Admin@123
    'System',
    'Administrator',
    1,
    GETUTCDATE(),
    GETUTCDATE()
);

SET IDENTITY_INSERT core.users OFF;

-- Assign admin role
INSERT INTO core.user_roles (user_id, role_id, assigned_at)
VALUES (1, 1, GETUTCDATE());

GO

-- =============================================
-- SEED SAMPLE ASSET CATEGORIES
-- =============================================

SET IDENTITY_INSERT assets.asset_categories ON;

INSERT INTO assets.asset_categories (id, name, code, description, parent_id, level, sort_order, is_active, created_by, created_at)
VALUES
    (1, 'HVAC Systems', 'HVAC', 'Heating, Ventilation, and Air Conditioning equipment', NULL, 0, 1, 1, 1, GETUTCDATE()),
    (2, 'Electrical Systems', 'ELEC', 'Electrical equipment and systems', NULL, 0, 2, 1, 1, GETUTCDATE()),
    (3, 'Plumbing Systems', 'PLMB', 'Plumbing equipment and fixtures', NULL, 0, 3, 1, 1, GETUTCDATE()),
    (4, 'Production Equipment', 'PROD', 'Manufacturing and production machinery', NULL, 0, 4, 1, 1, GETUTCDATE()),
    (5, 'IT Equipment', 'IT', 'Information technology hardware', NULL, 0, 5, 1, 1, GETUTCDATE()),
    (6, 'Vehicles', 'VEH', 'Company vehicles and transport equipment', NULL, 0, 6, 1, 1, GETUTCDATE()),
    -- Sub-categories
    (7, 'Air Handlers', 'HVAC-AH', 'Air handling units', 1, 1, 1, 1, 1, GETUTCDATE()),
    (8, 'Chillers', 'HVAC-CH', 'Chiller units', 1, 1, 2, 1, 1, GETUTCDATE()),
    (9, 'Boilers', 'HVAC-BO', 'Boiler systems', 1, 1, 3, 1, 1, GETUTCDATE()),
    (10, 'Transformers', 'ELEC-TR', 'Electrical transformers', 2, 1, 1, 1, 1, GETUTCDATE()),
    (11, 'Generators', 'ELEC-GN', 'Backup generators', 2, 1, 2, 1, 1, GETUTCDATE()),
    (12, 'Servers', 'IT-SRV', 'Server hardware', 5, 1, 1, 1, 1, GETUTCDATE()),
    (13, 'Network Equipment', 'IT-NET', 'Switches, routers, firewalls', 5, 1, 2, 1, 1, GETUTCDATE());

SET IDENTITY_INSERT assets.asset_categories OFF;
GO

-- =============================================
-- SEED SAMPLE LOCATIONS
-- =============================================

SET IDENTITY_INSERT assets.asset_locations ON;

INSERT INTO assets.asset_locations (id, name, code, description, parent_id, level, full_path, building, floor, is_active, created_by, created_at)
VALUES
    (1, 'Main Campus', 'MAIN', 'Primary facility location', NULL, 0, 'Main Campus', NULL, NULL, 1, 1, GETUTCDATE()),
    (2, 'Building A', 'MAIN-A', 'Administrative building', 1, 1, 'Main Campus > Building A', 'Building A', NULL, 1, 1, GETUTCDATE()),
    (3, 'Building B', 'MAIN-B', 'Production facility', 1, 1, 'Main Campus > Building B', 'Building B', NULL, 1, 1, GETUTCDATE()),
    (4, 'Building A - Floor 1', 'MAIN-A-F1', 'First floor', 2, 2, 'Main Campus > Building A > Floor 1', 'Building A', '1', 1, 1, GETUTCDATE()),
    (5, 'Building A - Floor 2', 'MAIN-A-F2', 'Second floor', 2, 2, 'Main Campus > Building A > Floor 2', 'Building A', '2', 1, 1, GETUTCDATE()),
    (6, 'Building B - Production Floor', 'MAIN-B-PF', 'Main production area', 3, 2, 'Main Campus > Building B > Production Floor', 'Building B', 'G', 1, 1, GETUTCDATE()),
    (7, 'Building B - Warehouse', 'MAIN-B-WH', 'Storage warehouse', 3, 2, 'Main Campus > Building B > Warehouse', 'Building B', 'G', 1, 1, GETUTCDATE()),
    (8, 'Data Center', 'MAIN-DC', 'IT data center', 1, 1, 'Main Campus > Data Center', 'Data Center', NULL, 1, 1, GETUTCDATE()),
    (9, 'Mechanical Room A1', 'MAIN-A-MR1', 'Mechanical room on floor 1', 4, 3, 'Main Campus > Building A > Floor 1 > Mechanical Room', 'Building A', '1', 1, 1, GETUTCDATE());

SET IDENTITY_INSERT assets.asset_locations OFF;
GO

PRINT 'CMMS Seed Data inserted successfully.';
GO
