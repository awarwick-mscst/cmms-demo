-- Work Order Tasks & Task Templates Migration (Reset)
-- Drops existing tables and recreates them

-- Drop index on PM schedules first
DROP INDEX IF EXISTS IX_preventive_maintenance_schedules_task_template_id ON [maintenance].[preventive_maintenance_schedules];

-- Drop FK constraint from PM schedules (find and drop by pattern)
DECLARE @constraintName NVARCHAR(200);
SELECT @constraintName = fk.name
FROM sys.foreign_keys fk
INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
WHERE fk.parent_object_id = OBJECT_ID('[maintenance].[preventive_maintenance_schedules]')
  AND COL_NAME(fkc.parent_object_id, fkc.parent_column_id) = 'task_template_id';

IF @constraintName IS NOT NULL
BEGIN
    DECLARE @sql NVARCHAR(500) = 'ALTER TABLE [maintenance].[preventive_maintenance_schedules] DROP CONSTRAINT ' + QUOTENAME(@constraintName);
    EXEC sp_executesql @sql;
END

-- Now drop the column if it exists
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('[maintenance].[preventive_maintenance_schedules]') AND name = 'task_template_id')
BEGIN
    ALTER TABLE [maintenance].[preventive_maintenance_schedules] DROP COLUMN [task_template_id];
END

-- Drop other indexes
DROP INDEX IF EXISTS IX_work_order_tasks_work_order_id ON [maintenance].[work_order_tasks];
DROP INDEX IF EXISTS IX_work_order_task_template_items_template_id ON [maintenance].[work_order_task_template_items];

-- Drop tables in correct order (child tables first)
DROP TABLE IF EXISTS [maintenance].[work_order_tasks];
DROP TABLE IF EXISTS [maintenance].[work_order_task_template_items];
DROP TABLE IF EXISTS [maintenance].[work_order_task_templates];

-- Task templates table
CREATE TABLE [maintenance].[work_order_task_templates] (
    [id] INT IDENTITY(1,1) PRIMARY KEY,
    [name] NVARCHAR(200) NOT NULL,
    [description] NVARCHAR(2000) NULL,
    [is_active] BIT NOT NULL DEFAULT 1,
    [created_at] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [created_by] INT NULL,
    [updated_at] DATETIME2 NULL,
    [updated_by] INT NULL,
    [is_deleted] BIT NOT NULL DEFAULT 0,
    [deleted_at] DATETIME2 NULL
);

-- Template items table
CREATE TABLE [maintenance].[work_order_task_template_items] (
    [id] INT IDENTITY(1,1) PRIMARY KEY,
    [template_id] INT NOT NULL REFERENCES [maintenance].[work_order_task_templates]([id]) ON DELETE CASCADE,
    [sort_order] INT NOT NULL DEFAULT 0,
    [description] NVARCHAR(1000) NOT NULL,
    [is_required] BIT NOT NULL DEFAULT 1
);

-- Work order tasks table
CREATE TABLE [maintenance].[work_order_tasks] (
    [id] INT IDENTITY(1,1) PRIMARY KEY,
    [work_order_id] INT NOT NULL REFERENCES [maintenance].[work_orders]([id]) ON DELETE CASCADE,
    [sort_order] INT NOT NULL DEFAULT 0,
    [description] NVARCHAR(1000) NOT NULL,
    [is_completed] BIT NOT NULL DEFAULT 0,
    [completed_at] DATETIME2 NULL,
    [completed_by_id] INT NULL REFERENCES [core].[users]([id]) ON DELETE SET NULL,
    [notes] NVARCHAR(2000) NULL,
    [is_required] BIT NOT NULL DEFAULT 1,
    [created_at] DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- Add template FK to PM schedules
ALTER TABLE [maintenance].[preventive_maintenance_schedules]
ADD [task_template_id] INT NULL REFERENCES [maintenance].[work_order_task_templates]([id]) ON DELETE SET NULL;

-- Create indexes
CREATE INDEX IX_work_order_tasks_work_order_id ON [maintenance].[work_order_tasks]([work_order_id]);
CREATE INDEX IX_work_order_task_template_items_template_id ON [maintenance].[work_order_task_template_items]([template_id]);
CREATE INDEX IX_preventive_maintenance_schedules_task_template_id ON [maintenance].[preventive_maintenance_schedules]([task_template_id]);

PRINT 'Work Order Tasks migration completed successfully';
