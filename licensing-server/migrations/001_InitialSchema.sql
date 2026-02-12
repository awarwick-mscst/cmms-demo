-- CMMS Licensing Server - Initial Schema
-- Database: CmmsLicensing

USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'CmmsLicensing')
BEGIN
    CREATE DATABASE CmmsLicensing;
END
GO

USE CmmsLicensing;
GO

-- Customers table
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'customers')
BEGIN
    CREATE TABLE customers (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        company_name NVARCHAR(200) NOT NULL,
        contact_name NVARCHAR(200) NOT NULL,
        contact_email NVARCHAR(200) NOT NULL,
        phone NVARCHAR(50) NULL,
        notes NVARCHAR(2000) NULL,
        is_active BIT NOT NULL DEFAULT 1,
        created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        updated_at DATETIME2 NULL
    );

    CREATE UNIQUE INDEX IX_customers_contact_email ON customers(contact_email);
END
GO

-- Licenses table
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'licenses')
BEGIN
    CREATE TABLE licenses (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        customer_id INT NOT NULL,
        license_key NVARCHAR(4000) NOT NULL,
        tier NVARCHAR(50) NOT NULL DEFAULT 'Basic',
        max_activations INT NOT NULL DEFAULT 1,
        issued_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        expires_at DATETIME2 NOT NULL,
        is_revoked BIT NOT NULL DEFAULT 0,
        revoked_at DATETIME2 NULL,
        revoked_reason NVARCHAR(500) NULL,
        notes NVARCHAR(2000) NULL,
        created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        updated_at DATETIME2 NULL,
        CONSTRAINT FK_licenses_customer FOREIGN KEY (customer_id) REFERENCES customers(Id)
    );

    CREATE UNIQUE INDEX IX_licenses_license_key ON licenses(license_key);
END
GO

-- License activations table
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'license_activations')
BEGIN
    CREATE TABLE license_activations (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        license_id INT NOT NULL,
        hardware_id NVARCHAR(256) NOT NULL,
        machine_name NVARCHAR(200) NOT NULL,
        os_info NVARCHAR(500) NULL,
        is_active BIT NOT NULL DEFAULT 1,
        activated_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        deactivated_at DATETIME2 NULL,
        last_phone_home DATETIME2 NULL,
        last_ip_address NVARCHAR(45) NULL,
        CONSTRAINT FK_activations_license FOREIGN KEY (license_id) REFERENCES licenses(Id) ON DELETE CASCADE
    );

    CREATE UNIQUE INDEX IX_activations_license_hardware
        ON license_activations(license_id, hardware_id)
        WHERE is_active = 1;
END
GO

-- License audit logs table
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'license_audit_logs')
BEGIN
    CREATE TABLE license_audit_logs (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        license_id INT NOT NULL,
        action NVARCHAR(100) NOT NULL,
        details NVARCHAR(2000) NULL,
        ip_address NVARCHAR(45) NULL,
        hardware_id NVARCHAR(256) NULL,
        [timestamp] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_audit_license FOREIGN KEY (license_id) REFERENCES licenses(Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_audit_timestamp ON license_audit_logs([timestamp]);
END
GO

PRINT 'CmmsLicensing schema created successfully.';
GO
