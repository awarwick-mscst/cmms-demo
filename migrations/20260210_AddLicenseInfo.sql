-- Add license_info table to CMMS database
-- Run: sqlcmd -S "FRAGBOX\SQLEXPRESS" -d CMMS -E -i "migrations/20260210_AddLicenseInfo.sql"

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'core' AND TABLE_NAME = 'license_info')
BEGIN
    CREATE TABLE core.license_info (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        license_key NVARCHAR(4000) NOT NULL,
        tier NVARCHAR(50) NOT NULL DEFAULT 'Basic',
        features NVARCHAR(1000) NOT NULL DEFAULT '',
        hardware_id NVARCHAR(256) NOT NULL,
        activation_id INT NULL,
        expires_at DATETIME2 NOT NULL,
        last_phone_home DATETIME2 NULL,
        last_phone_home_response NVARCHAR(4000) NULL,
        status NVARCHAR(50) NOT NULL DEFAULT 'NotActivated',
        warning_message NVARCHAR(500) NULL,
        created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        updated_at DATETIME2 NULL,
        created_by INT NULL,
        updated_by INT NULL,
        is_deleted BIT NOT NULL DEFAULT 0,
        deleted_at DATETIME2 NULL
    );

    PRINT 'Created core.license_info table';
END
ELSE
BEGIN
    PRINT 'core.license_info table already exists';
END
GO
