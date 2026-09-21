-- =============================================
-- Migration: Add invoice_type to tbt_sale_invoice_header
-- Date: 2026-09-21
-- Description: แยกใบแจ้งหนี้ขายสินค้า (PRODUCT, so_running = tbt_sale_order.so_number) กับ
--   ใบแจ้งหนี้ขายวัตถุดิบ (MATERIAL, so_running = tbt_sale_material_header.running เลข INVM)
--   ให้ออก invoice ในระบบเดียวกันได้ แต่แยกแยะที่มาของ so_running ได้ชัดเจน (TK202609160001)
-- Run order: รันได้อิสระ ไม่ผูกกับ migration อื่น
-- Re-run safety: Idempotent — ADD COLUMN IF NOT EXISTS + เช็ค pg_constraint ก่อนเพิ่ม CHECK +
--   CREATE UNIQUE INDEX IF NOT EXISTS, รันซ้ำได้ไม่ error
-- Data: แถวเดิมทั้งหมด (ใบแจ้งหนี้ขายสินค้า) ได้ค่า default 'PRODUCT' อัตโนมัติ ไม่ต้อง backfill
-- Impact: ต้อง deploy API พร้อมกันหรือหลัง SQL นี้เท่านั้น (API เก่าไม่รู้จักคอลัมน์นี้ยังทำงานได้ปกติ
--   แต่ API ใหม่ query ผ่าน EF จะ select invoice_type เสมอ — รัน SQL ก่อน deploy API เสมอ)
-- =============================================

ALTER TABLE tbt_sale_invoice_header ADD COLUMN IF NOT EXISTS invoice_type CHARACTER VARYING(20) NOT NULL DEFAULT 'PRODUCT';

COMMENT ON COLUMN tbt_sale_invoice_header.invoice_type IS 'PRODUCT = so_running คือ tbt_sale_order.so_number / MATERIAL = so_running คือ tbt_sale_material_header.running (เลข INVM)';

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'tbt_sale_invoice_header_invoice_type_ck'
    ) THEN
        ALTER TABLE tbt_sale_invoice_header
            ADD CONSTRAINT tbt_sale_invoice_header_invoice_type_ck CHECK (invoice_type IN ('PRODUCT', 'MATERIAL'));
    END IF;
END $$;

-- กันออกใบแจ้งหนี้ซ้ำระดับ DB: ใบสั่งขายวัตถุดิบ (SM) 1 ใบ ออก invoice ที่ยัง active ได้ใบเดียว
CREATE UNIQUE INDEX IF NOT EXISTS tbt_sale_invoice_header_material_so_running_uq
    ON tbt_sale_invoice_header (so_running)
    WHERE invoice_type = 'MATERIAL' AND is_delete = false;

-- verify: SELECT invoice_type, count(*) FROM tbt_sale_invoice_header GROUP BY 1;

-- =============================================
-- Rollback:
-- DROP INDEX IF EXISTS tbt_sale_invoice_header_material_so_running_uq;
-- ALTER TABLE tbt_sale_invoice_header DROP CONSTRAINT IF EXISTS tbt_sale_invoice_header_invoice_type_ck;
-- ALTER TABLE tbt_sale_invoice_header DROP COLUMN IF EXISTS invoice_type;
-- =============================================
