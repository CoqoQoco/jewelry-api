-- Rollback: 20261106_03_alter_tbt_production_pre_plan_material_add_ring_size.sql
-- ลบคอลัมน์ ring_size (ข้อมูลหาย) — รันหลัง rollback API แล้วเท่านั้น
ALTER TABLE tbt_production_pre_plan_material
    DROP COLUMN IF EXISTS ring_size;
