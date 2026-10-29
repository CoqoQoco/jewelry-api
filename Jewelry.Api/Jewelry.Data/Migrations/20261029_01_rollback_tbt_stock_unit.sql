-- =============================================
-- Rollback: Stock unit (TK202610290002)
-- Date: 2026-10-29
-- Description: ย้อนกลับ 20261029_01_create_tbt_stock_unit.sql
--   ลบ unit_id/unbind_reason, คืนดัชนี unique ต่อล็อต, ลบ tbt_stock_unit
--   ต้อง rollback API ก่อนรัน (โค้ดใหม่อ้างตาราง/คอลัมน์เหล่านี้)
--   คำเตือน: ถ้ามีแท็ก active มากกว่า 1 ต่อล็อตแล้ว (หลายชิ้นในล็อตเดียว)
--   การคืนดัชนี unique ต่อล็อตจะ error — ต้อง unbind ให้เหลือ 1 แท็กต่อล็อตก่อน
-- Re-run safety: Idempotent
-- =============================================

BEGIN;

DROP INDEX IF EXISTS idx_tbt_stock_piece_rfid_unit_active;
DROP INDEX IF EXISTS idx_tbt_stock_piece_rfid_lot;

ALTER TABLE tbt_stock_piece_rfid DROP CONSTRAINT IF EXISTS tbt_stock_piece_rfid_unit_fk;
ALTER TABLE tbt_stock_piece_rfid DROP COLUMN IF EXISTS unit_id;
ALTER TABLE tbt_stock_piece_rfid DROP COLUMN IF EXISTS unbind_reason;

CREATE UNIQUE INDEX IF NOT EXISTS idx_tbt_stock_piece_rfid_piece_active
    ON tbt_stock_piece_rfid (stock_number, product_code)
    WHERE is_active;

DROP TABLE IF EXISTS tbt_stock_unit;

COMMIT;
