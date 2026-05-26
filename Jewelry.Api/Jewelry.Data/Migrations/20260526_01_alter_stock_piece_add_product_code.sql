-- =============================================
-- Migration: Alter tbt_stock_piece — Add product_code + Composite PK
-- Date: 2026-05-26
-- Description: Phase A.1+A.2
--              A.1: เพิ่ม column product_code + backfill จาก tbt_sku.product_number
--                   ใช้ COALESCE(s.product_number, p.sku_code) เพื่อป้องกัน NULL
--                   กรณี product_number เป็น NULL ใช้ sku_code เป็น fallback
--              A.2: DROP PK เดิม (stock_number เดียว)
--                   ADD composite PK (stock_number, product_code)
-- Run order: ก่อน alter_stock_movement_add_product_code.sql
-- Re-run safety:
--   - ADD COLUMN IF NOT EXISTS — safe to re-run ถ้า column ยังไม่มี
--   - UPDATE + SET NOT NULL — ถ้า column มีค่าแล้ว UPDATE จะ skip (ค่าเดิมยังอยู่)
--   - DROP/ADD CONSTRAINT — ถ้ารัน 2 ครั้ง DROP จะ error (PK เดิมหายไปแล้ว)
--     แนะนำ: รัน script นี้ 1 ครั้งเท่านั้นใน maintenance window
-- =============================================

-- =============================================
-- A.1: Add product_code column
-- =============================================
ALTER TABLE tbt_stock_piece
    ADD COLUMN IF NOT EXISTS product_code CHARACTER VARYING;

-- backfill product_code จาก tbt_sku.product_number
-- COALESCE: ถ้า product_number เป็น NULL → ใช้ sku_code เป็น fallback
UPDATE tbt_stock_piece p
SET product_code = COALESCE(s.product_number, p.sku_code)
FROM tbt_sku s
WHERE s.sku_code = p.sku_code
  AND p.product_code IS NULL;

-- บังคับ NOT NULL หลัง backfill เสร็จ
ALTER TABLE tbt_stock_piece
    ALTER COLUMN product_code SET NOT NULL;

-- =============================================
-- A.2: Recreate PK เป็น composite (stock_number, product_code)
-- =============================================
-- ก่อน DROP PK ต้อง DROP dependent FK ใน tbt_stock_movement ก่อน
-- (script 02 จะ re-create เป็น composite FK)
ALTER TABLE tbt_stock_movement
    DROP CONSTRAINT IF EXISTS tbt_stock_movement_piece_fk;

ALTER TABLE tbt_stock_piece
    DROP CONSTRAINT IF EXISTS tbt_stock_piece_pk;

ALTER TABLE tbt_stock_piece
    ADD CONSTRAINT tbt_stock_piece_pk PRIMARY KEY (stock_number, product_code);

-- index บน product_code สำหรับ query by product
CREATE INDEX IF NOT EXISTS idx_piece_product_code ON tbt_stock_piece(product_code);
