-- Add unique index on Asset.Barcode column
-- Only applies to non-deleted assets with non-null barcodes

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_assets_barcode' AND object_id = OBJECT_ID('assets.assets'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [IX_assets_barcode]
    ON [assets].[assets] ([barcode])
    WHERE [is_deleted] = 0 AND [barcode] IS NOT NULL;

    PRINT 'Created unique index IX_assets_barcode on assets.assets';
END
ELSE
BEGIN
    PRINT 'Index IX_assets_barcode already exists';
END
GO
