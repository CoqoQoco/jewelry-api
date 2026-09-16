-- =============================================
-- Migration: Create tbt_stock_convert tables
-- Date: 2026-09-16
-- Description: ใบแปลงสินค้า (Stock Convert) — แปลงสินค้าที่มีอยู่ในสต็อก (SOURCE) เป็นสินค้าใหม่ (RESULT)
--   เช่น กำไลพลอยทับทิม แปลงเป็นกำไลเพชร — ปิดยอดต้นทาง (SOLD) แล้วสร้างเลขสต็อกใหม่ให้ผลลัพธ์
--   1. tbt_stock_convert_header — เอกสารแปลงสินค้า 1 ใบ
--   2. tbt_stock_convert_item   — รายการต้นทาง (SOURCE) / ผลลัพธ์ (RESULT) ของแต่ละใบ
-- Run order: ไม่มี dependency กับตารางอื่น (อ้างอิง tbt_stock_piece ผ่าน stock_number แบบไม่มี FK จริง
--   เพราะ piece ต้นทางถูกปิดยอด/สร้างใหม่ตลอดเวลาอยู่แล้ว ไม่ผูก FK กันลบ/ปิดยอดไม่ได้)
-- Re-run safety: Idempotent — CREATE TABLE/INDEX IF NOT EXISTS
-- =============================================

-- =============================================
-- 1. tbt_stock_convert_header
-- =============================================
CREATE TABLE IF NOT EXISTS tbt_stock_convert_header (
    running          CHARACTER VARYING NOT NULL,
    so_number        CHARACTER VARYING,
    so_line_key      CHARACTER VARYING,
    status           INT NOT NULL DEFAULT 0,
    -- Status: 0=กำลังแปลง, 1=เสร็จ, 9=ยกเลิก
    status_name      CHARACTER VARYING,
    convert_cost     NUMERIC NOT NULL DEFAULT 0,
    remark           CHARACTER VARYING,
    cancel_reason    CHARACTER VARYING,
    complete_date    TIMESTAMPTZ,
    create_date      TIMESTAMPTZ NOT NULL,
    create_by        CHARACTER VARYING NOT NULL,
    update_date      TIMESTAMPTZ,
    update_by        CHARACTER VARYING,
    CONSTRAINT tbt_stock_convert_header_pk PRIMARY KEY (running)
);

CREATE INDEX IF NOT EXISTS idx_stock_convert_header_so_number ON tbt_stock_convert_header(so_number);

-- =============================================
-- 2. tbt_stock_convert_item
--    role: SOURCE = สินค้าต้นทางที่ถูกแปลง (ปิดยอด), RESULT = สินค้าใหม่ที่เกิดจากการแปลง
-- =============================================
CREATE TABLE IF NOT EXISTS tbt_stock_convert_item (
    id               BIGSERIAL NOT NULL,
    header_running   CHARACTER VARYING NOT NULL,
    role             CHARACTER VARYING NOT NULL,
    -- Role: SOURCE=สินค้าต้นทาง, RESULT=สินค้าผลลัพธ์
    stock_number     CHARACTER VARYING NOT NULL,
    product_code     CHARACTER VARYING,
    sku_code         CHARACTER VARYING,
    location_code    CHARACTER VARYING,
    qty              NUMERIC NOT NULL,
    product_cost     NUMERIC,
    create_date      TIMESTAMPTZ NOT NULL,
    create_by        CHARACTER VARYING NOT NULL,
    CONSTRAINT tbt_stock_convert_item_pk PRIMARY KEY (id),
    CONSTRAINT tbt_stock_convert_item_header_fk FOREIGN KEY (header_running)
        REFERENCES tbt_stock_convert_header (running),
    CONSTRAINT tbt_stock_convert_item_header_role_stock_uq UNIQUE (header_running, role, stock_number)
);

CREATE INDEX IF NOT EXISTS idx_stock_convert_item_header_running ON tbt_stock_convert_item(header_running);
CREATE INDEX IF NOT EXISTS idx_stock_convert_item_stock_number ON tbt_stock_convert_item(stock_number);

-- =============================================
-- Rollback:
-- DROP INDEX IF EXISTS idx_stock_convert_item_stock_number;
-- DROP INDEX IF EXISTS idx_stock_convert_item_header_running;
-- DROP TABLE IF EXISTS tbt_stock_convert_item;
-- DROP INDEX IF EXISTS idx_stock_convert_header_so_number;
-- DROP TABLE IF EXISTS tbt_stock_convert_header;
-- =============================================
