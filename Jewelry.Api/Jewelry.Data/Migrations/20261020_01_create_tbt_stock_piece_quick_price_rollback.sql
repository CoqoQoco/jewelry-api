-- Rollback: 20261020_01_create_tbt_stock_piece_quick_price.sql
-- ลบตารางประวัติราคาตีด่วนทั้งหมด (ข้อมูลหาย) — รันหลัง rollback API แล้วเท่านั้น
DROP INDEX IF EXISTS idx_tbt_stock_piece_quick_price_stock;
DROP TABLE IF EXISTS tbt_stock_piece_quick_price;
