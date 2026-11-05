-- Rollback: 20261105_01_alter_tbt_production_pre_plan_item_add_ring_size.sql
-- ลบคอลัมน์ ring_size (ข้อมูลหาย) — รันหลัง rollback API แล้วเท่านั้น
ALTER TABLE tbt_production_pre_plan_item
    DROP COLUMN IF EXISTS ring_size;
