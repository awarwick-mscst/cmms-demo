-- Migration: Add Attachments Table
-- Date: 2026-02-05
-- Description: Creates the attachments table for storing file attachments for Assets, Parts, and Work Orders

-- Create attachments table
IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE t.name = 'attachments' AND s.name = 'core')
BEGIN
    CREATE TABLE [core].[attachments] (
        [id] INT IDENTITY(1,1) NOT NULL,
        [entity_type] NVARCHAR(50) NOT NULL,
        [entity_id] INT NOT NULL,
        [attachment_type] NVARCHAR(20) NOT NULL,
        [title] NVARCHAR(200) NOT NULL,
        [file_name] NVARCHAR(500) NOT NULL,
        [file_path] NVARCHAR(1000) NOT NULL,
        [file_size] BIGINT NOT NULL,
        [mime_type] NVARCHAR(100) NOT NULL,
        [description] NVARCHAR(2000) NULL,
        [display_order] INT NOT NULL DEFAULT 0,
        [is_primary] BIT NOT NULL DEFAULT 0,
        [uploaded_at] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [uploaded_by] INT NULL,
        [created_at] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [created_by] INT NULL,
        [updated_at] DATETIME2 NULL,
        [updated_by] INT NULL,
        [is_deleted] BIT NOT NULL DEFAULT 0,
        [deleted_at] DATETIME2 NULL,
        CONSTRAINT [PK_attachments] PRIMARY KEY CLUSTERED ([id] ASC),
        CONSTRAINT [FK_attachments_users_uploaded_by] FOREIGN KEY ([uploaded_by]) REFERENCES [core].[users]([id]) ON DELETE SET NULL
    );

    -- Create indexes
    CREATE NONCLUSTERED INDEX [IX_attachments_entity]
    ON [core].[attachments] ([entity_type], [entity_id])
    WHERE [is_deleted] = 0;

    CREATE NONCLUSTERED INDEX [IX_attachments_type]
    ON [core].[attachments] ([attachment_type]);

    PRINT 'Created table [core].[attachments]';
END
ELSE
BEGIN
    PRINT 'Table [core].[attachments] already exists';
END
GO
