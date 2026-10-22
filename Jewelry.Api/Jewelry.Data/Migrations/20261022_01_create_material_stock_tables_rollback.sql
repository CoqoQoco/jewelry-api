-- Rollback: 20261022_01_create_material_stock_tables.sql
-- ลบตารางนโยบาย/ขั้นต่ำคลังวัตถุดิบทั้งหมด (ข้อมูลหาย) — รันหลัง rollback API แล้วเท่านั้น
DROP TABLE IF EXISTS tbm_material_stock_min;
DROP TABLE IF EXISTS tbm_material_stock_policy;
