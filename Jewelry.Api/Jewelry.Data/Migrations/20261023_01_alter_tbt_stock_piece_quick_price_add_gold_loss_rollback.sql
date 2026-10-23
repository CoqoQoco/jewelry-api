-- Rollback: 20261023_01_alter_tbt_stock_piece_quick_price_add_gold_loss.sql
-- ลบคอลัมน์ Gold loss (ข้อมูลในคอลัมน์เหล่านี้หาย) — รันหลัง rollback API แล้วเท่านั้น
ALTER TABLE tbt_stock_piece_quick_price
    DROP COLUMN IF EXISTS gold_loss_percent,
    DROP COLUMN IF EXISTS gold_loss_cost_total,
    DROP COLUMN IF EXISTS gold_loss_total_price;
