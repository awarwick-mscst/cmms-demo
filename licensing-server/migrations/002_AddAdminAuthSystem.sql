USE CmmsLicensing;
GO

-- Admin users table
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'admin_users')
BEGIN
    CREATE TABLE admin_users (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        username NVARCHAR(100) NOT NULL,
        email NVARCHAR(200) NOT NULL,
        password_hash NVARCHAR(500) NOT NULL,
        require_mfa BIT NOT NULL DEFAULT 1,
        totp_enabled BIT NOT NULL DEFAULT 0,
        totp_secret_encrypted NVARCHAR(500) NULL,
        recovery_codes_encrypted NVARCHAR(2000) NULL,
        account_locked BIT NOT NULL DEFAULT 0,
        failed_login_attempts INT NOT NULL DEFAULT 0,
        locked_until DATETIME2 NULL,
        last_login_at DATETIME2 NULL,
        last_login_ip NVARCHAR(45) NULL,
        created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        updated_at DATETIME2 NULL
    );

    CREATE UNIQUE INDEX IX_admin_users_username ON admin_users(username);
    CREATE UNIQUE INDEX IX_admin_users_email ON admin_users(email);

    PRINT 'Created admin_users table.';
END
GO

-- FIDO2 credentials table
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'fido2_credentials')
BEGIN
    CREATE TABLE fido2_credentials (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        admin_user_id INT NOT NULL,
        credential_id VARBINARY(1024) NOT NULL,
        public_key VARBINARY(MAX) NOT NULL,
        signature_counter BIGINT NOT NULL DEFAULT 0,
        aaguid UNIQUEIDENTIFIER NULL,
        device_name NVARCHAR(200) NOT NULL,
        credential_type NVARCHAR(50) NOT NULL DEFAULT 'public-key',
        transports NVARCHAR(200) NULL,
        is_backup_eligible BIT NOT NULL DEFAULT 0,
        is_backup_device BIT NOT NULL DEFAULT 0,
        last_used_at DATETIME2 NULL,
        registered_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        revoked_at DATETIME2 NULL,
        CONSTRAINT FK_fido2_admin_user FOREIGN KEY (admin_user_id) REFERENCES admin_users(Id) ON DELETE CASCADE
    );

    CREATE UNIQUE INDEX IX_fido2_credential_id ON fido2_credentials(credential_id);
    CREATE INDEX IX_fido2_admin_user ON fido2_credentials(admin_user_id);

    PRINT 'Created fido2_credentials table.';
END
GO

-- Admin login audit logs table
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'admin_login_audit_logs')
BEGIN
    CREATE TABLE admin_login_audit_logs (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        admin_user_id INT NULL,
        username NVARCHAR(100) NOT NULL,
        success BIT NOT NULL,
        auth_method NVARCHAR(50) NOT NULL,
        failure_reason NVARCHAR(200) NULL,
        ip_address NVARCHAR(45) NOT NULL,
        user_agent NVARCHAR(500) NULL,
        [timestamp] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_audit_admin_user FOREIGN KEY (admin_user_id) REFERENCES admin_users(Id) ON DELETE SET NULL
    );

    CREATE INDEX IX_audit_timestamp ON admin_login_audit_logs([timestamp]);
    CREATE INDEX IX_audit_admin_user ON admin_login_audit_logs(admin_user_id);
    CREATE INDEX IX_audit_success ON admin_login_audit_logs(success);

    PRINT 'Created admin_login_audit_logs table.';
END
GO

PRINT 'Admin authentication system migration complete.';
GO
