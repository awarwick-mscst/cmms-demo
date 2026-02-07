-- Migration: Add is_primary column to attachments table
-- Date: 2026-02-05
-- Description: Adds the is_primary column if it doesn't exist

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- Add is_primary column if it doesn't exist
IF NOT EXISTS (
    SELECT * FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[core].[attachments]')
    AND name = 'is_primary'
)
BEGIN
    ALTER TABLE [core].[attachments]
    ADD [is_primary] BIT NOT NULL DEFAULT 0;

    PRINT 'Added column [is_primary] to [core].[attachments]';
END
ELSE
BEGIN
    PRINT 'Column [is_primary] already exists in [core].[attachments]';
END
GO
