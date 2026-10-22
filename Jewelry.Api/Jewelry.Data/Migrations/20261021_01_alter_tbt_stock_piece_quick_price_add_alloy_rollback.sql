-- Rollback: 20261021_01_alter_tbt_stock_piece_quick_price_add_alloy.sql
-- ลบคอลัมน์ Alloy / total_price (ข้อมูลในคอลัมน์เหล่านี้หาย) — รันหลัง rollback API แล้วเท่านั้น
ALTER TABLE tbt_stock_piece_quick_price
    DROP COLUMN IF EXISTS karat,
    DROP COLUMN IF EXISTS alloy_factor,
    DROP COLUMN IF EXISTS alloy_weight,
    DROP COLUMN IF EXISTS alloy_price,
    DROP COLUMN IF EXISTS gold_cost_total,
    DROP COLUMN IF EXISTS total_price;
