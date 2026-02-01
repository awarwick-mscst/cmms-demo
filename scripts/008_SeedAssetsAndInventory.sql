-- =============================================
-- Seed Data: Assets and Inventory
-- Creates realistic data for a manufacturing facility
-- =============================================

USE CMMS;
GO

-- =============================================
-- CLEANUP: Delete in correct order (children first)
-- =============================================

PRINT 'Cleaning up existing seed data...';

-- Delete part stock first (references parts and storage_locations)
DELETE FROM inventory.part_stock;

-- Delete asset_parts (references parts and assets)
DELETE FROM inventory.asset_parts;

-- Delete parts (references part_categories and suppliers)
DELETE FROM inventory.parts;

-- Delete storage locations (children first, then parents)
DELETE FROM inventory.storage_locations WHERE parent_id IS NOT NULL;
DELETE FROM inventory.storage_locations;

-- Delete part categories (children first, then parents)
DELETE FROM inventory.part_categories WHERE parent_id IS NOT NULL;
DELETE FROM inventory.part_categories;

-- Delete suppliers
DELETE FROM inventory.suppliers;

-- Delete work order related data (if exists)
IF OBJECT_ID('maintenance.work_sessions', 'U') IS NOT NULL DELETE FROM maintenance.work_sessions;
IF OBJECT_ID('maintenance.work_order_labor', 'U') IS NOT NULL DELETE FROM maintenance.work_order_labor;
IF OBJECT_ID('maintenance.work_order_comments', 'U') IS NOT NULL DELETE FROM maintenance.work_order_comments;
IF OBJECT_ID('maintenance.work_order_history', 'U') IS NOT NULL DELETE FROM maintenance.work_order_history;
IF OBJECT_ID('maintenance.work_orders', 'U') IS NOT NULL DELETE FROM maintenance.work_orders;
IF OBJECT_ID('maintenance.preventive_maintenance_schedules', 'U') IS NOT NULL DELETE FROM maintenance.preventive_maintenance_schedules;

-- Delete assets (references asset_categories and asset_locations)
DELETE FROM assets.assets;

-- Delete asset locations (children first, then parents)
DELETE FROM assets.asset_locations WHERE parent_id IS NOT NULL;
DELETE FROM assets.asset_locations;

-- Delete asset categories (children first, then parents)
DELETE FROM assets.asset_categories WHERE parent_id IS NOT NULL;
DELETE FROM assets.asset_categories;

PRINT 'Cleanup complete.';
GO

-- =============================================
-- ASSET CATEGORIES
-- =============================================

SET IDENTITY_INSERT assets.asset_categories ON;

-- Top-level categories
INSERT INTO assets.asset_categories (id, name, code, description, parent_id, level, sort_order, is_active, created_at)
VALUES
    (1, 'HVAC', 'HVAC', 'Heating, Ventilation, and Air Conditioning', NULL, 0, 1, 1, GETUTCDATE()),
    (2, 'Electrical', 'ELEC', 'Electrical Systems and Equipment', NULL, 0, 2, 1, GETUTCDATE()),
    (3, 'Plumbing', 'PLMB', 'Plumbing and Water Systems', NULL, 0, 3, 1, GETUTCDATE()),
    (4, 'Production Equipment', 'PROD', 'Manufacturing and Production Machinery', NULL, 0, 4, 1, GETUTCDATE()),
    (5, 'Material Handling', 'MATL', 'Conveyors, Forklifts, Cranes', NULL, 0, 5, 1, GETUTCDATE()),
    (6, 'Vehicles', 'VEH', 'Company Vehicles and Fleet', NULL, 0, 6, 1, GETUTCDATE());

SET IDENTITY_INSERT assets.asset_categories OFF;
GO

SET IDENTITY_INSERT assets.asset_categories ON;

-- Subcategories (after parents exist)
INSERT INTO assets.asset_categories (id, name, code, description, parent_id, level, sort_order, is_active, created_at)
VALUES
    (10, 'Air Handling Units', 'HVAC-AHU', 'Central air handling units', 1, 1, 1, 1, GETUTCDATE()),
    (11, 'Chillers', 'HVAC-CHL', 'Chiller units', 1, 1, 2, 1, GETUTCDATE()),
    (12, 'Exhaust Fans', 'HVAC-EXH', 'Exhaust and ventilation fans', 1, 1, 3, 1, GETUTCDATE()),
    (20, 'Motors', 'ELEC-MTR', 'Electric motors', 2, 1, 1, 1, GETUTCDATE()),
    (21, 'Panels', 'ELEC-PNL', 'Electrical panels and switchgear', 2, 1, 2, 1, GETUTCDATE()),
    (30, 'Pumps', 'PLMB-PMP', 'Water and process pumps', 3, 1, 1, 1, GETUTCDATE()),
    (40, 'CNC Machines', 'PROD-CNC', 'Computer numerical control machines', 4, 1, 1, 1, GETUTCDATE()),
    (41, 'Presses', 'PROD-PRS', 'Hydraulic and mechanical presses', 4, 1, 2, 1, GETUTCDATE()),
    (42, 'Compressors', 'PROD-CMP', 'Air compressors', 4, 1, 3, 1, GETUTCDATE()),
    (50, 'Conveyors', 'MATL-CNV', 'Belt and roller conveyors', 5, 1, 1, 1, GETUTCDATE()),
    (51, 'Forklifts', 'MATL-FRK', 'Forklift trucks', 5, 1, 2, 1, GETUTCDATE()),
    (60, 'Trucks', 'VEH-TRK', 'Company trucks and vans', 6, 1, 1, 1, GETUTCDATE());

SET IDENTITY_INSERT assets.asset_categories OFF;
GO

PRINT 'Created asset categories';

-- =============================================
-- ASSET LOCATIONS
-- =============================================

SET IDENTITY_INSERT assets.asset_locations ON;

-- Buildings (top level)
INSERT INTO assets.asset_locations (id, name, code, description, parent_id, level, full_path, building, floor, room, is_active, created_at)
VALUES
    (1, 'Main Plant', 'MAIN', 'Main manufacturing building', NULL, 0, 'Main Plant', 'Main Plant', NULL, NULL, 1, GETUTCDATE()),
    (2, 'Warehouse', 'WH', 'Storage warehouse', NULL, 0, 'Warehouse', 'Warehouse', NULL, NULL, 1, GETUTCDATE()),
    (3, 'Utility Building', 'UTIL', 'Mechanical and electrical utilities', NULL, 0, 'Utility Building', 'Utility', NULL, NULL, 1, GETUTCDATE());

SET IDENTITY_INSERT assets.asset_locations OFF;
GO

SET IDENTITY_INSERT assets.asset_locations ON;

-- Areas (after parents exist)
INSERT INTO assets.asset_locations (id, name, code, description, parent_id, level, full_path, building, floor, room, is_active, created_at)
VALUES
    (10, 'Production Floor', 'MAIN-PF', 'Main production area', 1, 1, 'Main Plant > Production Floor', 'Main Plant', '1', NULL, 1, GETUTCDATE()),
    (11, 'Machine Shop', 'MAIN-MS', 'CNC and machining area', 1, 1, 'Main Plant > Machine Shop', 'Main Plant', '1', 'MS', 1, GETUTCDATE()),
    (12, 'Packaging Area', 'MAIN-PKG', 'Packaging and shipping prep', 1, 1, 'Main Plant > Packaging Area', 'Main Plant', '1', 'PKG', 1, GETUTCDATE()),
    (13, 'Roof', 'MAIN-RF', 'Rooftop equipment', 1, 1, 'Main Plant > Roof', 'Main Plant', 'R', NULL, 1, GETUTCDATE()),
    (20, 'Receiving', 'WH-RCV', 'Receiving dock', 2, 1, 'Warehouse > Receiving', 'Warehouse', '1', 'RCV', 1, GETUTCDATE()),
    (21, 'Shipping', 'WH-SHP', 'Shipping dock', 2, 1, 'Warehouse > Shipping', 'Warehouse', '1', 'SHP', 1, GETUTCDATE()),
    (30, 'Mechanical Room', 'UTIL-MECH', 'Main mechanical room', 3, 1, 'Utility Building > Mechanical Room', 'Utility', '1', 'MECH', 1, GETUTCDATE()),
    (31, 'Electrical Room', 'UTIL-ELEC', 'Main electrical room', 3, 1, 'Utility Building > Electrical Room', 'Utility', '1', 'ELEC', 1, GETUTCDATE()),
    (32, 'Compressor Room', 'UTIL-COMP', 'Air compressor room', 3, 1, 'Utility Building > Compressor Room', 'Utility', '1', 'COMP', 1, GETUTCDATE());

SET IDENTITY_INSERT assets.asset_locations OFF;
GO

PRINT 'Created asset locations';

-- =============================================
-- ASSETS (using correct column names)
-- =============================================

SET IDENTITY_INSERT assets.assets ON;

INSERT INTO assets.assets (id, asset_tag, name, description, serial_number, model, manufacturer, category_id, location_id, status, criticality, purchase_date, purchase_cost, warranty_expiry, notes, created_at)
VALUES
    -- HVAC Equipment
    (1, 'AHU-001', 'Air Handler Unit 1', 'Main production floor air handler, 10,000 CFM', 'AHU2019-0445', 'AHU-10K', 'Trane', 10, 13, 'Active', 'High', '2019-03-15', 45000.00, '2029-03-15', 'Located on roof, serves production floor', GETUTCDATE()),
    (2, 'AHU-002', 'Air Handler Unit 2', 'Machine shop air handler, 5,000 CFM', 'AHU2019-0446', 'AHU-5K', 'Trane', 10, 13, 'Active', 'High', '2019-03-15', 28000.00, '2029-03-15', 'Located on roof, serves machine shop', GETUTCDATE()),
    (3, 'CHL-001', 'Chiller 1', '200-ton air-cooled chiller', 'CHL2020-1122', 'CGAM-200', 'Trane', 11, 30, 'Active', 'Critical', '2020-06-01', 125000.00, '2030-06-01', 'Primary chiller for facility', GETUTCDATE()),

    -- Production Equipment
    (4, 'CNC-001', 'CNC Mill 1', 'Haas VF-2 vertical machining center', 'HM2018-34521', 'VF-2', 'Haas', 40, 11, 'Active', 'High', '2018-01-20', 85000.00, '2023-01-20', '40-taper, 30HP spindle', GETUTCDATE()),
    (5, 'CNC-002', 'CNC Mill 2', 'Haas VF-4 vertical machining center', 'HM2021-45632', 'VF-4', 'Haas', 40, 11, 'Active', 'High', '2021-04-10', 110000.00, '2026-04-10', '40-taper, 30HP spindle, larger table', GETUTCDATE()),
    (6, 'CNC-003', 'CNC Lathe', 'Haas ST-20 CNC turning center', 'HL2020-22341', 'ST-20', 'Haas', 40, 11, 'Active', 'High', '2020-08-15', 72000.00, '2025-08-15', '8" chuck, live tooling', GETUTCDATE()),
    (7, 'PRS-001', 'Hydraulic Press 1', '100-ton hydraulic press', 'HP2015-8891', 'H-100', 'Dake', 41, 10, 'Active', 'Medium', '2015-05-20', 35000.00, NULL, 'Used for forming and pressing operations', GETUTCDATE()),
    (8, 'PRS-002', 'Hydraulic Press 2', '50-ton hydraulic press', 'HP2017-9923', 'H-50', 'Dake', 41, 10, 'Active', 'Medium', '2017-09-10', 18000.00, NULL, 'Smaller jobs and assembly press fits', GETUTCDATE()),
    (9, 'CMP-001', 'Air Compressor', '100HP rotary screw compressor', 'IRC2019-5567', 'R100i', 'Ingersoll Rand', 42, 32, 'Active', 'Critical', '2019-11-01', 42000.00, '2024-11-01', 'Primary plant air supply, VFD equipped', GETUTCDATE()),

    -- Material Handling
    (10, 'CNV-001', 'Main Conveyor', 'Production floor main conveyor line', 'CNV2020-1234', 'BC-200', 'Hytrol', 50, 10, 'Active', 'High', '2020-02-15', 55000.00, '2025-02-15', '200ft belt conveyor with VFD', GETUTCDATE()),
    (11, 'FLT-001', 'Forklift 1', '5000lb propane forklift', 'FLT2021-7789', 'GC25K', 'CAT', 51, 20, 'Active', 'Medium', '2021-07-01', 32000.00, '2024-07-01', 'Warehouse receiving forklift', GETUTCDATE()),
    (12, 'FLT-002', 'Forklift 2', '6000lb electric forklift', 'FLT2022-8834', 'E60XN', 'Hyster', 51, 10, 'Active', 'Medium', '2022-03-15', 45000.00, '2027-03-15', 'Production floor forklift, battery powered', GETUTCDATE()),

    -- Plumbing
    (13, 'PMP-001', 'Coolant Pump 1', 'Machine shop coolant circulation pump', 'PMP2019-3345', 'AMT-2870', 'AMT Pumps', 30, 11, 'Active', 'Medium', '2019-06-15', 2500.00, NULL, '3HP centrifugal pump', GETUTCDATE()),

    -- Electrical
    (14, 'MDP-001', 'Main Distribution Panel', '480V main electrical distribution', 'MDP2018-001', 'NF-2000', 'Square D', 21, 31, 'Active', 'Critical', '2018-01-01', 85000.00, NULL, '2000A main breaker, serves all facility', GETUTCDATE()),

    -- Vehicles
    (15, 'TRK-001', 'Company Truck', 'Ford F-250 service truck', '1FTBF2B64MEA12345', 'F-250 XLT', 'Ford', 60, 2, 'Active', 'Medium', '2022-06-01', 52000.00, '2025-06-01', 'Service truck for off-site maintenance, tool boxes installed', GETUTCDATE());

SET IDENTITY_INSERT assets.assets OFF;
GO

PRINT 'Created assets';

-- =============================================
-- SUPPLIERS (using correct column names)
-- =============================================

SET IDENTITY_INSERT inventory.suppliers ON;

INSERT INTO inventory.suppliers (id, name, code, contact_name, email, phone, address, city, state, postal_code, country, website, notes, is_active, created_at)
VALUES
    (1, 'Grainger', 'GRAINGER', 'Account Team', 'orders@grainger.com', '1-800-472-4643', '100 Grainger Pkwy', 'Lake Forest', 'IL', '60045', 'USA', 'https://www.grainger.com', 'Primary MRO supplier', 1, GETUTCDATE()),
    (2, 'McMaster-Carr', 'MCMASTER', 'Customer Service', 'sales@mcmaster.com', '630-833-0300', '600 N County Line Rd', 'Elmhurst', 'IL', '60126', 'USA', 'https://www.mcmaster.com', 'Fast delivery, comprehensive catalog', 1, GETUTCDATE()),
    (3, 'Motion Industries', 'MOTION', 'Local Rep', 'orders@motion.com', '1-800-526-9328', '1605 Alton Rd', 'Birmingham', 'AL', '35210', 'USA', 'https://www.motionindustries.com', 'Bearings and power transmission specialist', 1, GETUTCDATE()),
    (4, 'Fastenal', 'FASTENAL', 'Branch Manager', 'orders@fastenal.com', '507-454-5374', '2001 Theurer Blvd', 'Winona', 'MN', '55987', 'USA', 'https://www.fastenal.com', 'Fasteners and safety supplies', 1, GETUTCDATE()),
    (5, 'Ingersoll Rand Parts', 'IRPARTS', 'Parts Dept', 'parts@irco.com', '1-800-921-3173', '800-B Beaty St', 'Davidson', 'NC', '28036', 'USA', 'https://www.ingersollrand.com', 'OEM compressor parts', 1, GETUTCDATE());

SET IDENTITY_INSERT inventory.suppliers OFF;
GO

PRINT 'Created suppliers';

-- =============================================
-- PART CATEGORIES
-- =============================================

SET IDENTITY_INSERT inventory.part_categories ON;

-- Top-level categories
INSERT INTO inventory.part_categories (id, name, code, description, parent_id, is_active, created_at)
VALUES
    (1, 'Filters', 'FILT', 'Air, oil, and hydraulic filters', NULL, 1, GETUTCDATE()),
    (2, 'Bearings', 'BEAR', 'Ball bearings, roller bearings, bushings', NULL, 1, GETUTCDATE()),
    (3, 'Belts', 'BELT', 'V-belts, timing belts, flat belts', NULL, 1, GETUTCDATE()),
    (4, 'Electrical', 'ELEC', 'Electrical components and supplies', NULL, 1, GETUTCDATE()),
    (5, 'Lubricants', 'LUBE', 'Oils, greases, and lubricants', NULL, 1, GETUTCDATE()),
    (6, 'Safety', 'SAFE', 'PPE and safety supplies', NULL, 1, GETUTCDATE());

SET IDENTITY_INSERT inventory.part_categories OFF;
GO

SET IDENTITY_INSERT inventory.part_categories ON;

-- Subcategories
INSERT INTO inventory.part_categories (id, name, code, description, parent_id, is_active, created_at)
VALUES
    (10, 'HVAC Filters', 'FILT-HVAC', 'Air handling unit filters', 1, 1, GETUTCDATE()),
    (11, 'Oil Filters', 'FILT-OIL', 'Compressor and hydraulic oil filters', 1, 1, GETUTCDATE()),
    (20, 'Ball Bearings', 'BEAR-BALL', 'Deep groove ball bearings', 2, 1, GETUTCDATE()),
    (21, 'Mounted Bearings', 'BEAR-MNT', 'Pillow blocks and flange bearings', 2, 1, GETUTCDATE()),
    (40, 'Fuses', 'ELEC-FUSE', 'Fuses and fuse holders', 4, 1, GETUTCDATE()),
    (41, 'Contactors', 'ELEC-CONT', 'Motor starters and contactors', 4, 1, GETUTCDATE()),
    (60, 'Hand Protection', 'SAFE-HAND', 'Gloves and hand protection', 6, 1, GETUTCDATE());

SET IDENTITY_INSERT inventory.part_categories OFF;
GO

PRINT 'Created part categories';

-- =============================================
-- STORAGE LOCATIONS
-- =============================================

SET IDENTITY_INSERT inventory.storage_locations ON;

-- Main locations
INSERT INTO inventory.storage_locations (id, name, code, description, parent_id, is_active, created_at)
VALUES
    (1, 'Main Storeroom', 'MAIN-STORE', 'Primary parts storage', NULL, 1, GETUTCDATE()),
    (2, 'Maintenance Crib', 'MAINT-CRIB', 'Maintenance shop storage', NULL, 1, GETUTCDATE()),
    (3, 'HVAC Storage', 'HVAC-STORE', 'HVAC parts and filters', NULL, 1, GETUTCDATE());

SET IDENTITY_INSERT inventory.storage_locations OFF;
GO

SET IDENTITY_INSERT inventory.storage_locations ON;

-- Sub-locations (aisles)
INSERT INTO inventory.storage_locations (id, name, code, description, parent_id, is_active, created_at)
VALUES
    (10, 'Aisle A - Bearings', 'MAIN-A', 'Bearings and belts', 1, 1, GETUTCDATE()),
    (11, 'Aisle B - Electrical', 'MAIN-B', 'Electrical components', 1, 1, GETUTCDATE()),
    (12, 'Aisle C - Filters', 'MAIN-C', 'General filters', 1, 1, GETUTCDATE()),
    (13, 'Aisle D - Safety', 'MAIN-D', 'Safety and PPE', 1, 1, GETUTCDATE()),
    (20, 'HVAC Filter Rack', 'HVAC-FILT', 'Large format HVAC filters', 3, 1, GETUTCDATE());

SET IDENTITY_INSERT inventory.storage_locations OFF;
GO

PRINT 'Created storage locations';

-- =============================================
-- PARTS (using correct column names: supplier_id, min_stock_level, max_stock_level)
-- =============================================

SET IDENTITY_INSERT inventory.parts ON;

INSERT INTO inventory.parts (id, part_number, name, description, category_id, supplier_id, unit_of_measure, unit_cost, min_stock_level, max_stock_level, status, reorder_point, reorder_quantity, lead_time_days, manufacturer, manufacturer_part_number, notes, created_at)
VALUES
    -- Filters
    (1, 'FILT-20X20X2', 'Air Filter 20x20x2', 'Pleated air filter 20x20x2 MERV 8', 10, 1, 'Each', 8.50, 20, 100, 'Active', 30, 50, 3, 'Filtrete', '20x20x2-M8', 'AHU-001 pre-filter', GETUTCDATE()),
    (2, 'FILT-24X24X2', 'Air Filter 24x24x2', 'Pleated air filter 24x24x2 MERV 8', 10, 1, 'Each', 9.50, 15, 80, 'Active', 25, 40, 3, 'Filtrete', '24x24x2-M8', 'AHU-002 pre-filter', GETUTCDATE()),
    (3, 'FILT-COMP-IR', 'Compressor Air Filter', 'Intake filter for IR R100i compressor', 11, 5, 'Each', 45.00, 2, 10, 'Active', 4, 6, 7, 'Ingersoll Rand', '39588470', 'Critical - keep in stock', GETUTCDATE()),
    (4, 'FILT-OIL-IR', 'Compressor Oil Filter', 'Oil filter for IR R100i compressor', 11, 5, 'Each', 38.00, 2, 10, 'Active', 4, 6, 7, 'Ingersoll Rand', '39329602', 'Change every 2000 hours', GETUTCDATE()),

    -- Bearings
    (10, 'BRG-6205-2RS', 'Bearing 6205-2RS', 'Deep groove ball bearing 25x52x15mm sealed', 20, 3, 'Each', 12.50, 10, 50, 'Active', 15, 25, 5, 'SKF', '6205-2RS', 'Common motor bearing', GETUTCDATE()),
    (11, 'BRG-6206-2RS', 'Bearing 6206-2RS', 'Deep groove ball bearing 30x62x16mm sealed', 20, 3, 'Each', 14.00, 8, 40, 'Active', 12, 20, 5, 'SKF', '6206-2RS', 'Pump and motor bearing', GETUTCDATE()),
    (12, 'BRG-6207-2RS', 'Bearing 6207-2RS', 'Deep groove ball bearing 35x72x17mm sealed', 20, 3, 'Each', 16.50, 6, 30, 'Active', 10, 15, 5, 'SKF', '6207-2RS', 'Larger motor bearing', GETUTCDATE()),
    (13, 'BRG-UCP205', 'Pillow Block UCP205', 'Pillow block bearing unit 25mm bore', 21, 3, 'Each', 28.00, 4, 20, 'Active', 6, 10, 5, 'SKF', 'UCP205', 'Conveyor bearing', GETUTCDATE()),

    -- Belts
    (20, 'BELT-A68', 'V-Belt A68', 'Classical V-belt A68 4L700', 3, 2, 'Each', 8.75, 6, 30, 'Active', 10, 15, 3, 'Gates', 'A68', 'Common motor belt', GETUTCDATE()),
    (21, 'BELT-B75', 'V-Belt B75', 'Classical V-belt B75 5L780', 3, 2, 'Each', 11.25, 4, 24, 'Active', 8, 12, 3, 'Gates', 'B75', 'AHU blower belt', GETUTCDATE()),
    (22, 'BELT-B85', 'V-Belt B85', 'Classical V-belt B85 5L880', 3, 2, 'Each', 12.50, 4, 20, 'Active', 6, 10, 3, 'Gates', 'B85', 'Compressor belt', GETUTCDATE()),

    -- Electrical
    (30, 'CONT-30A-3P', 'Contactor 30A 3-Pole', '30A 3-pole contactor 120V coil', 41, 1, 'Each', 65.00, 3, 15, 'Active', 5, 8, 5, 'Square D', '8910DPA33V02', 'Motor starter contactor', GETUTCDATE()),
    (31, 'RELAY-ICE-8P', 'Ice Cube Relay 8-Pin', 'General purpose relay DPDT 120VAC', 4, 1, 'Each', 18.50, 10, 50, 'Active', 15, 25, 3, 'Square D', '8501KP12V20', 'Control relay', GETUTCDATE()),
    (32, 'FUSE-30A-CC', 'Fuse 30A Class CC', 'Time delay fuse 30A 600V Class CC', 40, 1, 'Each', 8.25, 20, 100, 'Active', 30, 50, 3, 'Bussmann', 'FNQ-30', 'Motor branch circuit', GETUTCDATE()),
    (33, 'FUSE-60A-J', 'Fuse 60A Class J', 'Time delay fuse 60A 600V Class J', 40, 1, 'Each', 22.00, 10, 50, 'Active', 15, 25, 3, 'Bussmann', 'LPJ-60SP', 'Main feeder protection', GETUTCDATE()),

    -- Lubricants
    (40, 'GRS-MOBIL-EP2', 'Grease Mobilux EP2', 'Lithium EP grease NLGI 2, 14oz cartridge', 5, 1, 'Each', 8.50, 20, 100, 'Active', 30, 50, 3, 'Mobil', 'Mobilux EP 2', '14oz cartridge', GETUTCDATE()),
    (41, 'OIL-HYD-5GAL', 'Hydraulic Oil AW46', 'ISO 46 anti-wear hydraulic oil', 5, 1, 'Each', 28.00, 10, 20, 'Active', 5, 40, 3, 'Mobil', 'DTE 25', '5 gallon pail', GETUTCDATE()),

    -- Safety
    (50, 'GLOVE-LEATHER', 'Leather Work Gloves', 'Heavy duty leather work gloves, L', 60, 4, 'Pair', 12.50, 25, 50, 'Active', 12, 100, 3, 'Wells Lamont', '1132L', 'General purpose', GETUTCDATE()),
    (51, 'GLASS-SAFETY', 'Safety Glasses', 'Clear lens safety glasses', 60, 4, 'Each', 4.50, 30, 60, 'Active', 15, 120, 3, '3M', 'SecureFit 400', 'Anti-fog', GETUTCDATE()),
    (52, 'PLUG-EAR', 'Ear Plugs Box', 'Foam ear plugs, 200 pair box', 60, 4, 'Box', 35.00, 5, 10, 'Active', 3, 20, 3, '3M', '1100', 'NRR 29', GETUTCDATE());

SET IDENTITY_INSERT inventory.parts OFF;
GO

PRINT 'Created parts';

-- =============================================
-- PART STOCK (initial inventory)
-- =============================================

INSERT INTO inventory.part_stock (part_id, location_id, quantity_on_hand, quantity_reserved, last_count_date, created_at)
VALUES
    -- Filters
    (1, 20, 45, 0, GETUTCDATE(), GETUTCDATE()),
    (2, 20, 32, 0, GETUTCDATE(), GETUTCDATE()),
    (1, 12, 20, 0, GETUTCDATE(), GETUTCDATE()),
    (2, 12, 15, 0, GETUTCDATE(), GETUTCDATE()),
    (3, 12, 6, 0, GETUTCDATE(), GETUTCDATE()),
    (4, 12, 15, 0, GETUTCDATE(), GETUTCDATE()),

    -- Bearings
    (10, 10, 35, 2, GETUTCDATE(), GETUTCDATE()),
    (11, 10, 28, 0, GETUTCDATE(), GETUTCDATE()),
    (12, 10, 18, 0, GETUTCDATE(), GETUTCDATE()),
    (13, 10, 12, 0, GETUTCDATE(), GETUTCDATE()),

    -- Belts
    (20, 10, 18, 0, GETUTCDATE(), GETUTCDATE()),
    (21, 10, 12, 0, GETUTCDATE(), GETUTCDATE()),
    (22, 10, 10, 0, GETUTCDATE(), GETUTCDATE()),
    (21, 3, 4, 0, GETUTCDATE(), GETUTCDATE()),

    -- Electrical
    (30, 11, 8, 0, GETUTCDATE(), GETUTCDATE()),
    (31, 11, 22, 0, GETUTCDATE(), GETUTCDATE()),
    (32, 11, 40, 0, GETUTCDATE(), GETUTCDATE()),
    (33, 11, 18, 0, GETUTCDATE(), GETUTCDATE()),

    -- Lubricants
    (40, 2, 45, 0, GETUTCDATE(), GETUTCDATE()),
    (41, 2, 12, 0, GETUTCDATE(), GETUTCDATE()),

    -- Safety
    (50, 13, 40, 0, GETUTCDATE(), GETUTCDATE()),
    (51, 13, 50, 0, GETUTCDATE(), GETUTCDATE()),
    (52, 13, 8, 0, GETUTCDATE(), GETUTCDATE());
GO

PRINT 'Created part stock';

PRINT '';
PRINT '==============================================';
PRINT 'Seed data completed successfully!';
PRINT '==============================================';
PRINT '';
PRINT 'Created:';
PRINT '  - 14 Asset Categories (including Vehicles)';
PRINT '  - 12 Asset Locations';
PRINT '  - 15 Assets (HVAC, CNC, Presses, Forklifts, Truck)';
PRINT '  - 5 Suppliers';
PRINT '  - 13 Part Categories';
PRINT '  - 8 Storage Locations';
PRINT '  - 21 Parts (with min/max/reorder levels)';
PRINT '  - 23 Stock records';
GO
