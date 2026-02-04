-- Migration: Add ConnectionType, Language, and WindowsPrinterName columns to label_printers table
-- Run this script against your CMMS database to add support for EPL printers and Windows printer connections

-- Add new columns
ALTER TABLE [admin].[label_printers]
ADD [connection_type] NVARCHAR(20) NOT NULL DEFAULT 'Network',
    [language] NVARCHAR(10) NOT NULL DEFAULT 'ZPL',
    [windows_printer_name] NVARCHAR(200) NULL;

-- Make ip_address nullable (not required for Windows printer connections)
ALTER TABLE [admin].[label_printers]
ALTER COLUMN [ip_address] NVARCHAR(50) NULL;

GO

-- Verify the changes
SELECT
    c.name AS ColumnName,
    t.name AS DataType,
    c.max_length,
    c.is_nullable
FROM sys.columns c
JOIN sys.types t ON c.user_type_id = t.user_type_id
WHERE c.object_id = OBJECT_ID('admin.label_printers')
ORDER BY c.column_id;
