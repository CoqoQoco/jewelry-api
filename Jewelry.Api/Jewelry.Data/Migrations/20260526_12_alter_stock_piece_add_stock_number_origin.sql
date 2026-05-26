-- =============================================
-- Migration: Alter tbt_stock_piece — Add stock_number_origin
-- Date: 2026-05-26
-- Description: เพิ่ม column stock_number_origin ใน piece (= legacy product_code)
--              UI "เลขที่ผลิต (เก่า)" (e.g. AB66079) เก็บใน legacy.product_code
--              ไม่ได้ migrate ใน Phase A เพราะชื่อ field ทับซ้อนกับ piece.product_code ใหม่
--              backfill จาก tbt_stock_product.product_code
-- Run order: หลัง Phase A (piece มี product_code + composite PK แล้ว)
-- Re-run safety:
--   - ADD COLUMN IF NOT EXISTS — idempotent
--   - UPDATE WHERE IS NULL — idempotent
-- =============================================

ALTER TABLE tbt_stock_piece
    ADD COLUMN IF NOT EXISTS stock_number_origin CHARACTER VARYING;

UPDATE tbt_stock_piece p
SET stock_number_origin = sp.product_code
FROM tbt_stock_product sp
WHERE sp.stock_number = p.stock_number
  AND p.stock_number_origin IS NULL;

CREATE INDEX IF NOT EXISTS idx_piece_stock_number_origin
    ON tbt_stock_piece(stock_number_origin);
