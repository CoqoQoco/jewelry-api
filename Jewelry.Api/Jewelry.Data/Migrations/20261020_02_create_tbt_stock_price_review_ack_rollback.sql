-- Rollback: 20261020_02_create_tbt_stock_price_review_ack.sql
-- ลบตารางประวัติรับทราบราคาทั้งหมด (ข้อมูลหาย) — รันหลัง rollback API แล้วเท่านั้น
DROP INDEX IF EXISTS idx_tbt_stock_price_review_ack_stock;
DROP TABLE IF EXISTS tbt_stock_price_review_ack;
