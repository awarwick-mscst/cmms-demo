-- =============================================
-- Inventory Roles and Permissions Migration
-- Adds Inventory Manager and Purchaser roles
-- =============================================

USE CMMS;
GO

-- =============================================
-- ADD INVENTORY PERMISSIONS
-- =============================================

-- Get the next permission ID
DECLARE @nextPermId INT = (SELECT ISNULL(MAX(id), 0) + 1 FROM core.permissions);

-- Add inventory permissions if they don't exist
IF NOT EXISTS (SELECT 1 FROM core.permissions WHERE name = 'inventory.view')
BEGIN
    INSERT INTO core.permissions (name, description, module, created_at)
    VALUES
        ('inventory.view', 'View parts, stock levels, and transactions', 'Inventory', GETUTCDATE()),
        ('inventory.manage', 'Manage parts, adjust stock, and process transactions', 'Inventory', GETUTCDATE()),
        ('inventory.receive', 'Receive parts into inventory', 'Inventory', GETUTCDATE()),
        ('inventory.issue', 'Issue parts from inventory', 'Inventory', GETUTCDATE()),
        ('inventory.transfer', 'Transfer parts between locations', 'Inventory', GETUTCDATE()),
        ('inventory.adjust', 'Adjust stock levels (count corrections)', 'Inventory', GETUTCDATE()),
        ('suppliers.view', 'View suppliers', 'Inventory', GETUTCDATE()),
        ('suppliers.manage', 'Create, edit, delete suppliers', 'Inventory', GETUTCDATE()),
        ('purchase-orders.view', 'View purchase orders', 'Inventory', GETUTCDATE()),
        ('purchase-orders.create', 'Create purchase orders', 'Inventory', GETUTCDATE()),
        ('purchase-orders.approve', 'Approve purchase orders', 'Inventory', GETUTCDATE());

    PRINT 'Added inventory permissions';
END
GO

-- =============================================
-- ADD INVENTORY ROLES
-- =============================================

-- Inventory Manager role
IF NOT EXISTS (SELECT 1 FROM core.roles WHERE name = 'Inventory Manager')
BEGIN
    INSERT INTO core.roles (name, description, is_system_role, created_at)
    VALUES ('Inventory Manager', 'Full inventory management including receiving, issuing, and reporting', 1, GETUTCDATE());

    PRINT 'Added Inventory Manager role';
END
GO

-- Purchaser role
IF NOT EXISTS (SELECT 1 FROM core.roles WHERE name = 'Purchaser')
BEGIN
    INSERT INTO core.roles (name, description, is_system_role, created_at)
    VALUES ('Purchaser', 'Create and manage purchase orders, receive inventory', 1, GETUTCDATE());

    PRINT 'Added Purchaser role';
END
GO

-- Storekeeper role (issues parts to technicians)
IF NOT EXISTS (SELECT 1 FROM core.roles WHERE name = 'Storekeeper')
BEGIN
    INSERT INTO core.roles (name, description, is_system_role, created_at)
    VALUES ('Storekeeper', 'Issue parts from inventory, perform stock counts', 1, GETUTCDATE());

    PRINT 'Added Storekeeper role';
END
GO

-- =============================================
-- ASSIGN PERMISSIONS TO ROLES
-- =============================================

-- Inventory Manager - All inventory permissions
INSERT INTO core.role_permissions (role_id, permission_id)
SELECT r.id, p.id
FROM core.roles r, core.permissions p
WHERE r.name = 'Inventory Manager'
  AND p.module = 'Inventory'
  AND NOT EXISTS (
      SELECT 1 FROM core.role_permissions rp
      WHERE rp.role_id = r.id AND rp.permission_id = p.id
  );

-- Purchaser - View, receive, suppliers, purchase orders
INSERT INTO core.role_permissions (role_id, permission_id)
SELECT r.id, p.id
FROM core.roles r, core.permissions p
WHERE r.name = 'Purchaser'
  AND p.name IN ('inventory.view', 'inventory.receive', 'suppliers.view', 'suppliers.manage',
                 'purchase-orders.view', 'purchase-orders.create')
  AND NOT EXISTS (
      SELECT 1 FROM core.role_permissions rp
      WHERE rp.role_id = r.id AND rp.permission_id = p.id
  );

-- Storekeeper - View, issue, transfer, adjust
INSERT INTO core.role_permissions (role_id, permission_id)
SELECT r.id, p.id
FROM core.roles r, core.permissions p
WHERE r.name = 'Storekeeper'
  AND p.name IN ('inventory.view', 'inventory.issue', 'inventory.transfer', 'inventory.adjust')
  AND NOT EXISTS (
      SELECT 1 FROM core.role_permissions rp
      WHERE rp.role_id = r.id AND rp.permission_id = p.id
  );

-- Technician - View inventory and issue parts (for work orders)
INSERT INTO core.role_permissions (role_id, permission_id)
SELECT r.id, p.id
FROM core.roles r, core.permissions p
WHERE r.name = 'Technician'
  AND p.name IN ('inventory.view', 'inventory.issue')
  AND NOT EXISTS (
      SELECT 1 FROM core.role_permissions rp
      WHERE rp.role_id = r.id AND rp.permission_id = p.id
  );

-- Manager - View inventory and suppliers
INSERT INTO core.role_permissions (role_id, permission_id)
SELECT r.id, p.id
FROM core.roles r, core.permissions p
WHERE r.name = 'Manager'
  AND p.name IN ('inventory.view', 'inventory.manage', 'suppliers.view', 'suppliers.manage')
  AND NOT EXISTS (
      SELECT 1 FROM core.role_permissions rp
      WHERE rp.role_id = r.id AND rp.permission_id = p.id
  );

-- Administrator - All inventory permissions (they should already have all, but just in case)
INSERT INTO core.role_permissions (role_id, permission_id)
SELECT r.id, p.id
FROM core.roles r, core.permissions p
WHERE r.name = 'Administrator'
  AND p.module = 'Inventory'
  AND NOT EXISTS (
      SELECT 1 FROM core.role_permissions rp
      WHERE rp.role_id = r.id AND rp.permission_id = p.id
  );

GO

PRINT 'Inventory roles and permissions migration completed successfully.';
GO
