-- Migration: Add Notification System Tables
-- Date: 2026-02-07
-- Description: Creates tables for email notifications, calendar sync, and user preferences

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- Create NotificationQueue table
IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE t.name = 'notification_queue' AND s.name = 'core')
BEGIN
    CREATE TABLE [core].[notification_queue] (
        [id] INT IDENTITY(1,1) NOT NULL,
        [type] INT NOT NULL,
        [recipient_user_id] INT NULL,
        [recipient_email] NVARCHAR(256) NOT NULL,
        [subject] NVARCHAR(500) NOT NULL,
        [body] NVARCHAR(MAX) NOT NULL,
        [body_html] NVARCHAR(MAX) NULL,
        [status] INT NOT NULL DEFAULT 0,
        [retry_count] INT NOT NULL DEFAULT 0,
        [scheduled_for] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [processed_at] DATETIME2 NULL,
        [error_message] NVARCHAR(MAX) NULL,
        [reference_type] NVARCHAR(50) NULL,
        [reference_id] INT NULL,
        [created_at] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [created_by] INT NULL,
        [updated_at] DATETIME2 NULL,
        [updated_by] INT NULL,
        [is_deleted] BIT NOT NULL DEFAULT 0,
        [deleted_at] DATETIME2 NULL,
        CONSTRAINT [PK_notification_queue] PRIMARY KEY CLUSTERED ([id] ASC),
        CONSTRAINT [FK_notification_queue_users_recipient] FOREIGN KEY ([recipient_user_id]) REFERENCES [core].[users]([id]) ON DELETE SET NULL
    );

    -- Create indexes
    CREATE NONCLUSTERED INDEX [IX_notification_queue_status_scheduled]
    ON [core].[notification_queue] ([status], [scheduled_for])
    WHERE [is_deleted] = 0;

    CREATE NONCLUSTERED INDEX [IX_notification_queue_recipient]
    ON [core].[notification_queue] ([recipient_user_id])
    WHERE [is_deleted] = 0;

    CREATE NONCLUSTERED INDEX [IX_notification_queue_reference]
    ON [core].[notification_queue] ([reference_type], [reference_id])
    WHERE [is_deleted] = 0;

    PRINT 'Created table [core].[notification_queue]';
END
ELSE
BEGIN
    PRINT 'Table [core].[notification_queue] already exists';
END
GO

-- Create NotificationLog table
IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE t.name = 'notification_log' AND s.name = 'core')
BEGIN
    CREATE TABLE [core].[notification_log] (
        [id] INT IDENTITY(1,1) NOT NULL,
        [type] INT NOT NULL,
        [recipient_email] NVARCHAR(256) NOT NULL,
        [subject] NVARCHAR(500) NOT NULL,
        [channel] INT NOT NULL,
        [success] BIT NOT NULL,
        [external_message_id] NVARCHAR(256) NULL,
        [error_message] NVARCHAR(MAX) NULL,
        [sent_at] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [queue_id] INT NULL,
        [reference_type] NVARCHAR(50) NULL,
        [reference_id] INT NULL,
        [created_at] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_notification_log] PRIMARY KEY CLUSTERED ([id] ASC),
        CONSTRAINT [FK_notification_log_queue] FOREIGN KEY ([queue_id]) REFERENCES [core].[notification_queue]([id]) ON DELETE SET NULL
    );

    -- Create indexes
    CREATE NONCLUSTERED INDEX [IX_notification_log_sent_at]
    ON [core].[notification_log] ([sent_at] DESC);

    CREATE NONCLUSTERED INDEX [IX_notification_log_type]
    ON [core].[notification_log] ([type]);

    CREATE NONCLUSTERED INDEX [IX_notification_log_reference]
    ON [core].[notification_log] ([reference_type], [reference_id]);

    PRINT 'Created table [core].[notification_log]';
END
ELSE
BEGIN
    PRINT 'Table [core].[notification_log] already exists';
END
GO

-- Create UserNotificationPreferences table
IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE t.name = 'user_notification_preferences' AND s.name = 'core')
BEGIN
    CREATE TABLE [core].[user_notification_preferences] (
        [id] INT IDENTITY(1,1) NOT NULL,
        [user_id] INT NOT NULL,
        [notification_type] INT NOT NULL,
        [email_enabled] BIT NOT NULL DEFAULT 1,
        [calendar_enabled] BIT NOT NULL DEFAULT 1,
        [created_at] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [created_by] INT NULL,
        [updated_at] DATETIME2 NULL,
        [updated_by] INT NULL,
        [is_deleted] BIT NOT NULL DEFAULT 0,
        [deleted_at] DATETIME2 NULL,
        CONSTRAINT [PK_user_notification_preferences] PRIMARY KEY CLUSTERED ([id] ASC),
        CONSTRAINT [FK_user_notification_preferences_users] FOREIGN KEY ([user_id]) REFERENCES [core].[users]([id]) ON DELETE CASCADE,
        CONSTRAINT [UQ_user_notification_preferences] UNIQUE ([user_id], [notification_type])
    );

    -- Create index
    CREATE NONCLUSTERED INDEX [IX_user_notification_preferences_user]
    ON [core].[user_notification_preferences] ([user_id])
    WHERE [is_deleted] = 0;

    PRINT 'Created table [core].[user_notification_preferences]';
END
ELSE
BEGIN
    PRINT 'Table [core].[user_notification_preferences] already exists';
END
GO

-- Create IntegrationSettings table
IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE t.name = 'integration_settings' AND s.name = 'core')
BEGIN
    CREATE TABLE [core].[integration_settings] (
        [id] INT IDENTITY(1,1) NOT NULL,
        [provider_type] NVARCHAR(50) NOT NULL,
        [setting_key] NVARCHAR(100) NOT NULL,
        [encrypted_value] NVARCHAR(MAX) NOT NULL,
        [expires_at] DATETIME2 NULL,
        [created_at] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [created_by] INT NULL,
        [updated_at] DATETIME2 NULL,
        [updated_by] INT NULL,
        [is_deleted] BIT NOT NULL DEFAULT 0,
        [deleted_at] DATETIME2 NULL,
        CONSTRAINT [PK_integration_settings] PRIMARY KEY CLUSTERED ([id] ASC),
        CONSTRAINT [UQ_integration_settings] UNIQUE ([provider_type], [setting_key])
    );

    -- Create index
    CREATE NONCLUSTERED INDEX [IX_integration_settings_provider]
    ON [core].[integration_settings] ([provider_type])
    WHERE [is_deleted] = 0;

    PRINT 'Created table [core].[integration_settings]';
END
ELSE
BEGIN
    PRINT 'Table [core].[integration_settings] already exists';
END
GO

-- Create CalendarEvent table
IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE t.name = 'calendar_events' AND s.name = 'core')
BEGIN
    CREATE TABLE [core].[calendar_events] (
        [id] INT IDENTITY(1,1) NOT NULL,
        [external_event_id] NVARCHAR(256) NOT NULL,
        [calendar_type] NVARCHAR(20) NOT NULL,
        [user_id] INT NULL,
        [reference_type] NVARCHAR(50) NOT NULL,
        [reference_id] INT NOT NULL,
        [title] NVARCHAR(500) NOT NULL,
        [start_time] DATETIME2 NOT NULL,
        [end_time] DATETIME2 NOT NULL,
        [provider_type] NVARCHAR(50) NOT NULL DEFAULT 'MicrosoftGraph',
        [created_at] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [created_by] INT NULL,
        [updated_at] DATETIME2 NULL,
        [updated_by] INT NULL,
        [is_deleted] BIT NOT NULL DEFAULT 0,
        [deleted_at] DATETIME2 NULL,
        CONSTRAINT [PK_calendar_events] PRIMARY KEY CLUSTERED ([id] ASC),
        CONSTRAINT [FK_calendar_events_users] FOREIGN KEY ([user_id]) REFERENCES [core].[users]([id]) ON DELETE SET NULL
    );

    -- Create indexes
    CREATE NONCLUSTERED INDEX [IX_calendar_events_external]
    ON [core].[calendar_events] ([external_event_id])
    WHERE [is_deleted] = 0;

    CREATE NONCLUSTERED INDEX [IX_calendar_events_reference]
    ON [core].[calendar_events] ([reference_type], [reference_id])
    WHERE [is_deleted] = 0;

    CREATE NONCLUSTERED INDEX [IX_calendar_events_user]
    ON [core].[calendar_events] ([user_id])
    WHERE [is_deleted] = 0;

    PRINT 'Created table [core].[calendar_events]';
END
ELSE
BEGIN
    PRINT 'Table [core].[calendar_events] already exists';
END
GO

PRINT 'Notification system migration completed successfully';
GO
