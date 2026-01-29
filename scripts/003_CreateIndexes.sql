-- CMMS Additional Indexes Script
-- SQL Server 2022
-- Performance optimization indexes

USE CMMS;
GO

-- =============================================
-- FULL-TEXT SEARCH SETUP (Optional)
-- =============================================

-- Create full-text catalog if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.fulltext_catalogs WHERE name = 'CMMS_FullText')
BEGIN
    CREATE FULLTEXT CATALOG CMMS_FullText AS DEFAULT;
END
GO

-- Full-text index on assets for search
IF NOT EXISTS (SELECT * FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID('assets.assets'))
BEGIN
    CREATE FULLTEXT INDEX ON assets.assets(name, description, notes)
    KEY INDEX PK__assets__3213E83F0BC6C43E -- Replace with actual PK name
    ON CMMS_FullText
    WITH CHANGE_TRACKING AUTO;
END
GO

-- =============================================
-- COVERING INDEXES FOR COMMON QUERIES
-- =============================================

-- Asset list query optimization
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'ix_assets_list_cover')
BEGIN
    CREATE NONCLUSTERED INDEX ix_assets_list_cover
    ON assets.assets (status, category_id, location_id)
    INCLUDE (asset_tag, name, manufacturer, model, criticality, created_at)
    WHERE is_deleted = 0;
END
GO

-- Asset by category with details
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'ix_assets_by_category_cover')
BEGIN
    CREATE NONCLUSTERED INDEX ix_assets_by_category_cover
    ON assets.assets (category_id)
    INCLUDE (asset_tag, name, status, location_id, manufacturer, model)
    WHERE is_deleted = 0;
END
GO

-- User lookup optimization
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'ix_users_lookup')
BEGIN
    CREATE NONCLUSTERED INDEX ix_users_lookup
    ON core.users (is_active, is_deleted)
    INCLUDE (username, email, first_name, last_name, is_locked)
    WHERE is_deleted = 0;
END
GO

-- Refresh token validation
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'ix_refresh_tokens_validation')
BEGIN
    CREATE NONCLUSTERED INDEX ix_refresh_tokens_validation
    ON core.refresh_tokens (token_hash, expires_at)
    INCLUDE (user_id, revoked_at)
    WHERE revoked_at IS NULL;
END
GO

-- =============================================
-- STATISTICS UPDATE
-- =============================================

-- Update statistics for better query plans
UPDATE STATISTICS core.users;
UPDATE STATISTICS core.roles;
UPDATE STATISTICS core.permissions;
UPDATE STATISTICS assets.assets;
UPDATE STATISTICS assets.asset_categories;
UPDATE STATISTICS assets.asset_locations;

GO

PRINT 'CMMS Additional Indexes created successfully.';
GO
