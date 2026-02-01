-- =============================================
-- Work Sessions Migration Script
-- Adds support for active work tracking
-- =============================================

-- Create work_sessions table
IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE t.name = 'work_sessions' AND s.name = 'maintenance')
BEGIN
    CREATE TABLE maintenance.work_sessions (
        id INT IDENTITY(1,1) PRIMARY KEY,
        work_order_id INT NOT NULL,
        user_id INT NOT NULL,
        started_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        ended_at DATETIME2 NULL,
        hours_worked DECIMAL(10,2) NULL,
        notes NVARCHAR(2000) NULL,
        is_active BIT NOT NULL DEFAULT 1,
        created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_work_sessions_work_order FOREIGN KEY (work_order_id) REFERENCES maintenance.work_orders(id) ON DELETE CASCADE,
        CONSTRAINT FK_work_sessions_user FOREIGN KEY (user_id) REFERENCES core.users(id)
    );

    CREATE INDEX IX_work_sessions_user_active ON maintenance.work_sessions(user_id, is_active) WHERE is_active = 1;
    CREATE INDEX IX_work_sessions_work_order ON maintenance.work_sessions(work_order_id, is_active);

    PRINT 'Created maintenance.work_sessions table';
END
GO

PRINT 'Work Sessions migration completed successfully.';
GO
