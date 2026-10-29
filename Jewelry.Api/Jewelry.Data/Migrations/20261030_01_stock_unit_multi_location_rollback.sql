-- =============================================
-- Rollback: ล็อตเดียวหลายคลัง (TK202610290003)
-- Date: 2026-10-30
-- Description: ย้อนกลับ 20261030_01_stock_unit_multi_location.sql
--   ลบ tbt_stock_movement_unit และคอลัมน์ reserved_* ของ tbt_stock_unit
--   ต้อง rollback API ก่อนรัน (โค้ดใหม่อ้างตาราง/คอลัมน์เหล่านี้)
--   คำเตือน: ถ้ามีล็อตที่ชิ้นกระจายหลายคลังไปแล้ว ต้องย้ายชิ้นกลับให้อยู่คลังเดียวกับ
--   tbt_stock_piece.location_code ก่อน (โค้ดเวอร์ชัน TK202610290002 บังคับทุกชิ้นอยู่คลังเดียวกับล็อต
--   และ balance รายคลังจะไม่ตรงถ้าไม่ย้ายกลับ) — ตรวจด้วย:
--     SELECT p.stock_number FROM tbt_stock_piece p JOIN tbt_stock_unit u
--       ON u.stock_number = p.stock_number AND u.product_code = p.product_code
--      WHERE u.status <> 'SOLD' AND u.location_code <> p.location_code;
-- Re-run safety: Idempotent
-- =============================================

BEGIN;

DROP TABLE IF EXISTS tbt_stock_movement_unit;

DROP INDEX IF EXISTS idx_tbt_stock_unit_reserved_line;

ALTER TABLE tbt_stock_unit DROP COLUMN IF EXISTS reserved_line_id;
ALTER TABLE tbt_stock_unit DROP COLUMN IF EXISTS reserved_ref;
ALTER TABLE tbt_stock_unit DROP COLUMN IF EXISTS reserved_ref_type;

COMMIT;
