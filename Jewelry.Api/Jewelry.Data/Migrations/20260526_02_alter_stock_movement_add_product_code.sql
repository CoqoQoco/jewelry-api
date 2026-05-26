-- =============================================
-- Migration: Alter tbt_stock_movement — Add product_code + Composite FK
-- Date: 2026-05-26
-- Description: Phase A.3
--              เพิ่ม column product_code ใน tbt_stock_movement
--              backfill จาก tbt_stock_piece (join by stock_number)
--              DROP FK เดิม (stock_number เดียว)
--              ADD composite FK (stock_number, product_code) → tbt_stock_piece
-- Run order: หลัง alter_stock_piece_add_product_code.sql (piece ต้องมี composite PK ก่อน)
-- Re-run safety:
--   - ADD COLUMN IF NOT EXISTS — safe
--   - UPDATE — ถ้า product_code มีค่าแล้ว WHERE clause กรอง skip
--   - DROP/ADD CONSTRAINT — รัน 1 ครั้งเท่านั้นใน maintenance window
-- =============================================

-- =============================================
-- A.3: Add product_code column
-- =============================================
ALTER TABLE tbt_stock_movement
    ADD COLUMN IF NOT EXISTS product_code CHARACTER VARYING;

-- backfill product_code จาก tbt_stock_piece (join by stock_number)
-- rows ที่ stock_number เป็น NULL (balance-only movements) จะไม่ถูก UPDATE
UPDATE tbt_stock_movement m
SET product_code = p.product_code
FROM tbt_stock_piece p
WHERE p.stock_number = m.stock_number
  AND m.product_code IS NULL;

-- บังคับ NOT NULL เฉพาะ rows ที่มี stock_number
-- (rows ที่ stock_number IS NULL อนุญาต product_code เป็น NULL ได้ — เป็น aggregate movement)
-- ถ้า business rule กำหนดว่าทุก movement ต้องมี product_code → เปลี่ยนเป็น SET NOT NULL
ALTER TABLE tbt_stock_movement
    ALTER COLUMN product_code SET NOT NULL;

-- =============================================
-- DROP FK เดิม (single stock_number)
-- =============================================
ALTER TABLE tbt_stock_movement
    DROP CONSTRAINT IF EXISTS tbt_stock_movement_piece_fk;

-- =============================================
-- ADD composite FK (stock_number, product_code) → tbt_stock_piece
-- =============================================
ALTER TABLE tbt_stock_movement
    ADD CONSTRAINT tbt_stock_movement_piece_fk
        FOREIGN KEY (stock_number, product_code)
        REFERENCES tbt_stock_piece(stock_number, product_code);

-- index บน (stock_number, product_code) สำหรับ join
CREATE INDEX IF NOT EXISTS idx_movement_piece_composite
    ON tbt_stock_movement(stock_number, product_code);
