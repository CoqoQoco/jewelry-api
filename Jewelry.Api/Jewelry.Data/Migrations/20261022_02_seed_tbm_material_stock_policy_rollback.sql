-- Rollback: 20261022_02_seed_tbm_material_stock_policy.sql
-- ลบแถวนโยบายเริ่มต้น (API จะ fallback เป็นค่า default ในโค้ด)
DELETE FROM tbm_material_stock_policy WHERE material_kind IN ('GOLD', 'GEM');
