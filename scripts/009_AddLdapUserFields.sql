-- =============================================
-- Migration: Add LDAP/Active Directory User Fields
-- Version: 009
-- Description: Adds columns to support LDAP/AD authentication
-- =============================================

USE [CMMS];
GO

-- Check if columns already exist before adding
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[core].[users]') AND name = 'is_ldap_user')
BEGIN
    ALTER TABLE [core].[users]
    ADD [is_ldap_user] BIT NOT NULL DEFAULT 0;

    PRINT 'Added column: is_ldap_user';
END
ELSE
BEGIN
    PRINT 'Column is_ldap_user already exists';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[core].[users]') AND name = 'ldap_distinguished_name')
BEGIN
    ALTER TABLE [core].[users]
    ADD [ldap_distinguished_name] NVARCHAR(500) NULL;

    PRINT 'Added column: ldap_distinguished_name';
END
ELSE
BEGIN
    PRINT 'Column ldap_distinguished_name already exists';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[core].[users]') AND name = 'ldap_last_sync_at')
BEGIN
    ALTER TABLE [core].[users]
    ADD [ldap_last_sync_at] DATETIME2 NULL;

    PRINT 'Added column: ldap_last_sync_at';
END
ELSE
BEGIN
    PRINT 'Column ldap_last_sync_at already exists';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[core].[users]') AND name = 'authentication_type')
BEGIN
    ALTER TABLE [core].[users]
    ADD [authentication_type] INT NOT NULL DEFAULT 0;

    PRINT 'Added column: authentication_type';
END
ELSE
BEGIN
    PRINT 'Column authentication_type already exists';
END
GO

-- Add index for LDAP DN lookups
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[core].[users]') AND name = 'IX_users_ldap_distinguished_name')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_users_ldap_distinguished_name]
    ON [core].[users] ([ldap_distinguished_name])
    WHERE [ldap_distinguished_name] IS NOT NULL AND [is_deleted] = 0;

    PRINT 'Created index: IX_users_ldap_distinguished_name';
END
ELSE
BEGIN
    PRINT 'Index IX_users_ldap_distinguished_name already exists';
END
GO

-- Add index for LDAP user lookups
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[core].[users]') AND name = 'IX_users_is_ldap_user')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_users_is_ldap_user]
    ON [core].[users] ([is_ldap_user])
    WHERE [is_deleted] = 0;

    PRINT 'Created index: IX_users_is_ldap_user';
END
ELSE
BEGIN
    PRINT 'Index IX_users_is_ldap_user already exists';
END
GO

-- Update existing users to have Local authentication type (0)
-- This ensures backward compatibility
UPDATE [core].[users]
SET [authentication_type] = 0,
    [is_ldap_user] = 0
WHERE [authentication_type] IS NULL OR [is_ldap_user] IS NULL;
GO

PRINT 'LDAP user fields migration completed successfully';
GO

-- =============================================
-- Authentication Type Values:
-- 0 = Local (password stored in database)
-- 1 = Ldap (authenticate against LDAP/AD only)
-- 2 = Both (try LDAP first, fall back to local)
-- =============================================
