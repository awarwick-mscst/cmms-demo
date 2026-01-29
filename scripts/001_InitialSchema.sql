-- CMMS Database Initial Schema
-- SQL Server 2022
-- Run this script against your SQL Server instance to create the database and tables

-- Create database if it doesn't exist
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'CMMS')
BEGIN
    CREATE DATABASE CMMS;
END
GO

USE CMMS;
GO

-- Create schemas
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'core')
BEGIN
    EXEC('CREATE SCHEMA core');
END
GO

IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'assets')
BEGIN
    EXEC('CREATE SCHEMA assets');
END
GO

-- =============================================
-- CORE SCHEMA TABLES
-- =============================================

-- Roles table
CREATE TABLE core.roles (
    id INT IDENTITY(1,1) PRIMARY KEY,
    name NVARCHAR(50) NOT NULL UNIQUE,
    description NVARCHAR(255) NULL,
    is_system_role BIT NOT NULL DEFAULT 0,
    created_at DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    updated_at DATETIME2(7) NULL
);

-- Permissions table
CREATE TABLE core.permissions (
    id INT IDENTITY(1,1) PRIMARY KEY,
    name NVARCHAR(100) NOT NULL UNIQUE,
    description NVARCHAR(255) NULL,
    module NVARCHAR(50) NOT NULL,
    created_at DATETIME2(7) NOT NULL DEFAULT GETUTCDATE()
);

-- Role-Permissions junction table
CREATE TABLE core.role_permissions (
    role_id INT NOT NULL,
    permission_id INT NOT NULL,
    created_at DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT pk_role_permissions PRIMARY KEY (role_id, permission_id),
    CONSTRAINT fk_role_permissions_role FOREIGN KEY (role_id) REFERENCES core.roles(id) ON DELETE CASCADE,
    CONSTRAINT fk_role_permissions_permission FOREIGN KEY (permission_id) REFERENCES core.permissions(id) ON DELETE CASCADE
);

-- Users table
CREATE TABLE core.users (
    id INT IDENTITY(1,1) PRIMARY KEY,
    username NVARCHAR(50) NOT NULL UNIQUE,
    email NVARCHAR(255) NOT NULL UNIQUE,
    password_hash NVARCHAR(255) NOT NULL,
    first_name NVARCHAR(100) NOT NULL,
    last_name NVARCHAR(100) NOT NULL,
    phone NVARCHAR(20) NULL,
    is_active BIT NOT NULL DEFAULT 1,
    is_locked BIT NOT NULL DEFAULT 0,
    failed_login_attempts INT NOT NULL DEFAULT 0,
    lockout_end DATETIME2(7) NULL,
    last_login_at DATETIME2(7) NULL,
    password_changed_at DATETIME2(7) NULL,
    created_at DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    updated_at DATETIME2(7) NULL,
    created_by INT NULL,
    updated_by INT NULL,
    is_deleted BIT NOT NULL DEFAULT 0,
    deleted_at DATETIME2(7) NULL,
    CONSTRAINT fk_users_created_by FOREIGN KEY (created_by) REFERENCES core.users(id),
    CONSTRAINT fk_users_updated_by FOREIGN KEY (updated_by) REFERENCES core.users(id)
);

-- User-Roles junction table
CREATE TABLE core.user_roles (
    user_id INT NOT NULL,
    role_id INT NOT NULL,
    assigned_at DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    assigned_by INT NULL,
    CONSTRAINT pk_user_roles PRIMARY KEY (user_id, role_id),
    CONSTRAINT fk_user_roles_user FOREIGN KEY (user_id) REFERENCES core.users(id) ON DELETE CASCADE,
    CONSTRAINT fk_user_roles_role FOREIGN KEY (role_id) REFERENCES core.roles(id) ON DELETE CASCADE,
    CONSTRAINT fk_user_roles_assigned_by FOREIGN KEY (assigned_by) REFERENCES core.users(id)
);

-- Refresh tokens table
CREATE TABLE core.refresh_tokens (
    id INT IDENTITY(1,1) PRIMARY KEY,
    user_id INT NOT NULL,
    token NVARCHAR(500) NOT NULL,
    token_hash NVARCHAR(255) NOT NULL,
    expires_at DATETIME2(7) NOT NULL,
    created_at DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    created_by_ip NVARCHAR(45) NULL,
    revoked_at DATETIME2(7) NULL,
    revoked_by_ip NVARCHAR(45) NULL,
    replaced_by_token NVARCHAR(500) NULL,
    is_active AS (CASE WHEN revoked_at IS NULL AND expires_at > GETUTCDATE() THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END),
    CONSTRAINT fk_refresh_tokens_user FOREIGN KEY (user_id) REFERENCES core.users(id) ON DELETE CASCADE
);

-- Audit logs table
CREATE TABLE core.audit_logs (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    user_id INT NULL,
    action NVARCHAR(50) NOT NULL,
    entity_type NVARCHAR(100) NOT NULL,
    entity_id NVARCHAR(50) NULL,
    old_values NVARCHAR(MAX) NULL,
    new_values NVARCHAR(MAX) NULL,
    ip_address NVARCHAR(45) NULL,
    user_agent NVARCHAR(500) NULL,
    created_at DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT fk_audit_logs_user FOREIGN KEY (user_id) REFERENCES core.users(id)
);

-- =============================================
-- ASSETS SCHEMA TABLES
-- =============================================

-- Asset categories table
CREATE TABLE assets.asset_categories (
    id INT IDENTITY(1,1) PRIMARY KEY,
    name NVARCHAR(100) NOT NULL,
    code NVARCHAR(20) NOT NULL UNIQUE,
    description NVARCHAR(500) NULL,
    parent_id INT NULL,
    level INT NOT NULL DEFAULT 0,
    sort_order INT NOT NULL DEFAULT 0,
    is_active BIT NOT NULL DEFAULT 1,
    created_at DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    updated_at DATETIME2(7) NULL,
    created_by INT NULL,
    updated_by INT NULL,
    is_deleted BIT NOT NULL DEFAULT 0,
    deleted_at DATETIME2(7) NULL,
    CONSTRAINT fk_asset_categories_parent FOREIGN KEY (parent_id) REFERENCES assets.asset_categories(id),
    CONSTRAINT fk_asset_categories_created_by FOREIGN KEY (created_by) REFERENCES core.users(id),
    CONSTRAINT fk_asset_categories_updated_by FOREIGN KEY (updated_by) REFERENCES core.users(id)
);

-- Asset locations table (hierarchical)
CREATE TABLE assets.asset_locations (
    id INT IDENTITY(1,1) PRIMARY KEY,
    name NVARCHAR(100) NOT NULL,
    code NVARCHAR(20) NOT NULL UNIQUE,
    description NVARCHAR(500) NULL,
    parent_id INT NULL,
    level INT NOT NULL DEFAULT 0,
    full_path NVARCHAR(500) NULL,
    building NVARCHAR(100) NULL,
    floor NVARCHAR(20) NULL,
    room NVARCHAR(50) NULL,
    latitude DECIMAL(10, 8) NULL,
    longitude DECIMAL(11, 8) NULL,
    is_active BIT NOT NULL DEFAULT 1,
    created_at DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    updated_at DATETIME2(7) NULL,
    created_by INT NULL,
    updated_by INT NULL,
    is_deleted BIT NOT NULL DEFAULT 0,
    deleted_at DATETIME2(7) NULL,
    CONSTRAINT fk_asset_locations_parent FOREIGN KEY (parent_id) REFERENCES assets.asset_locations(id),
    CONSTRAINT fk_asset_locations_created_by FOREIGN KEY (created_by) REFERENCES core.users(id),
    CONSTRAINT fk_asset_locations_updated_by FOREIGN KEY (updated_by) REFERENCES core.users(id)
);

-- Assets table
CREATE TABLE assets.assets (
    id INT IDENTITY(1,1) PRIMARY KEY,
    asset_tag NVARCHAR(50) NOT NULL UNIQUE,
    name NVARCHAR(200) NOT NULL,
    description NVARCHAR(MAX) NULL,
    category_id INT NOT NULL,
    location_id INT NULL,
    status NVARCHAR(20) NOT NULL DEFAULT 'Active',
    criticality NVARCHAR(20) NOT NULL DEFAULT 'Medium',
    manufacturer NVARCHAR(100) NULL,
    model NVARCHAR(100) NULL,
    serial_number NVARCHAR(100) NULL,
    barcode NVARCHAR(100) NULL,
    purchase_date DATE NULL,
    purchase_cost DECIMAL(18, 2) NULL,
    warranty_expiry DATE NULL,
    expected_life_years INT NULL,
    installation_date DATE NULL,
    last_maintenance_date DATE NULL,
    next_maintenance_date DATE NULL,
    parent_asset_id INT NULL,
    assigned_to INT NULL,
    notes NVARCHAR(MAX) NULL,
    custom_fields NVARCHAR(MAX) NULL,
    created_at DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    updated_at DATETIME2(7) NULL,
    created_by INT NULL,
    updated_by INT NULL,
    is_deleted BIT NOT NULL DEFAULT 0,
    deleted_at DATETIME2(7) NULL,
    CONSTRAINT fk_assets_category FOREIGN KEY (category_id) REFERENCES assets.asset_categories(id),
    CONSTRAINT fk_assets_location FOREIGN KEY (location_id) REFERENCES assets.asset_locations(id),
    CONSTRAINT fk_assets_parent FOREIGN KEY (parent_asset_id) REFERENCES assets.assets(id),
    CONSTRAINT fk_assets_assigned_to FOREIGN KEY (assigned_to) REFERENCES core.users(id),
    CONSTRAINT fk_assets_created_by FOREIGN KEY (created_by) REFERENCES core.users(id),
    CONSTRAINT fk_assets_updated_by FOREIGN KEY (updated_by) REFERENCES core.users(id),
    CONSTRAINT chk_assets_status CHECK (status IN ('Active', 'Inactive', 'InMaintenance', 'Retired', 'Disposed')),
    CONSTRAINT chk_assets_criticality CHECK (criticality IN ('Critical', 'High', 'Medium', 'Low'))
);

-- Asset documents table
CREATE TABLE assets.asset_documents (
    id INT IDENTITY(1,1) PRIMARY KEY,
    asset_id INT NOT NULL,
    document_type NVARCHAR(50) NOT NULL,
    title NVARCHAR(200) NOT NULL,
    file_name NVARCHAR(255) NOT NULL,
    file_path NVARCHAR(500) NOT NULL,
    file_size BIGINT NULL,
    mime_type NVARCHAR(100) NULL,
    description NVARCHAR(500) NULL,
    uploaded_at DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    uploaded_by INT NULL,
    is_deleted BIT NOT NULL DEFAULT 0,
    deleted_at DATETIME2(7) NULL,
    CONSTRAINT fk_asset_documents_asset FOREIGN KEY (asset_id) REFERENCES assets.assets(id) ON DELETE CASCADE,
    CONSTRAINT fk_asset_documents_uploaded_by FOREIGN KEY (uploaded_by) REFERENCES core.users(id)
);

-- =============================================
-- INDEXES
-- =============================================

-- Core schema indexes
CREATE INDEX ix_users_email ON core.users(email) WHERE is_deleted = 0;
CREATE INDEX ix_users_username ON core.users(username) WHERE is_deleted = 0;
CREATE INDEX ix_users_is_active ON core.users(is_active) WHERE is_deleted = 0;
CREATE INDEX ix_refresh_tokens_user_id ON core.refresh_tokens(user_id);
CREATE INDEX ix_refresh_tokens_token_hash ON core.refresh_tokens(token_hash);
CREATE INDEX ix_audit_logs_user_id ON core.audit_logs(user_id);
CREATE INDEX ix_audit_logs_entity ON core.audit_logs(entity_type, entity_id);
CREATE INDEX ix_audit_logs_created_at ON core.audit_logs(created_at);

-- Assets schema indexes
CREATE INDEX ix_asset_categories_parent ON assets.asset_categories(parent_id) WHERE is_deleted = 0;
CREATE INDEX ix_asset_categories_code ON assets.asset_categories(code) WHERE is_deleted = 0;
CREATE INDEX ix_asset_locations_parent ON assets.asset_locations(parent_id) WHERE is_deleted = 0;
CREATE INDEX ix_asset_locations_code ON assets.asset_locations(code) WHERE is_deleted = 0;
CREATE INDEX ix_assets_asset_tag ON assets.assets(asset_tag) WHERE is_deleted = 0;
CREATE INDEX ix_assets_category ON assets.assets(category_id) WHERE is_deleted = 0;
CREATE INDEX ix_assets_location ON assets.assets(location_id) WHERE is_deleted = 0;
CREATE INDEX ix_assets_status ON assets.assets(status) WHERE is_deleted = 0;
CREATE INDEX ix_assets_assigned_to ON assets.assets(assigned_to) WHERE is_deleted = 0;
CREATE INDEX ix_asset_documents_asset ON assets.asset_documents(asset_id) WHERE is_deleted = 0;

PRINT 'CMMS Initial Schema created successfully.';
GO
