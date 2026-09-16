-- =============================================
-- Migration: Create Sale Order Deposit tables
-- Date: 2026-09-16
-- Description: มัดจำที่รับบนใบสั่งขาย (SO) ก่อนออก invoice — 1 SO รับมัดจำได้หลายครั้ง
--              และหักเข้าใบแจ้งหนี้ได้หลายใบ/หลายครั้ง (FIFO ตาม deposit_date)
--              เดิมมัดจำพิมพ์เป็นตัวเลขตรงบน invoice header (deposit) เท่านั้น — พอลบ/สร้าง invoice ใหม่
--              ยอดมัดจำหายไปด้วย เพราะไม่มีที่เก็บอิสระจาก invoice
--   1. tbt_sale_order_deposit        — มัดจำที่รับแต่ละครั้ง (1 แถวต่อ 1 ครั้งที่รับเงิน)
--   2. tbt_sale_order_deposit_apply  — ส่วนของมัดจำที่ถูกหักเข้า invoice แต่ละใบ
-- Run order: ไม่มี dependency กับตารางอื่นสำหรับ (1); (2) ต้องรันหลัง (1) และหลัง tbt_sale_invoice_header มีอยู่แล้ว
-- Re-run safety: Idempotent — CREATE TABLE/INDEX IF NOT EXISTS
-- =============================================

-- =============================================
-- 1. tbt_sale_order_deposit
-- =============================================
CREATE TABLE IF NOT EXISTS tbt_sale_order_deposit (
    running          CHARACTER VARYING NOT NULL,
    so_number        CHARACTER VARYING NOT NULL,
    deposit_date     TIMESTAMPTZ NOT NULL,
    amount           NUMERIC NOT NULL,
    currency_unit    CHARACTER VARYING,
    currency_rate    NUMERIC,
    payment          INT,
    -- Payment: 1=Cash, 2=Transfer, 3=Cheque (ตามชุดค่าเดียวกับ tbt_sale_invoice_payment_item.payment)
    payment_name     CHARACTER VARYING,
    bank_code        CHARACTER VARYING,
    bank_branch      CHARACTER VARYING,
    reference_number CHARACTER VARYING,
    image_path       CHARACTER VARYING,
    remark           CHARACTER VARYING,
    is_delete        BOOLEAN NOT NULL DEFAULT false,
    delete_reason    CHARACTER VARYING,
    create_date      TIMESTAMPTZ NOT NULL,
    create_by        CHARACTER VARYING NOT NULL,
    update_date      TIMESTAMPTZ,
    update_by        CHARACTER VARYING,
    CONSTRAINT tbt_sale_order_deposit_pk PRIMARY KEY (running),
    CONSTRAINT tbt_sale_order_deposit_amount_ck CHECK (amount > 0)
);

CREATE INDEX IF NOT EXISTS idx_tbt_sale_order_deposit_so_number ON tbt_sale_order_deposit(so_number);

COMMENT ON COLUMN tbt_sale_order_deposit.amount IS 'จำนวนเงินมัดจำที่รับ หน่วยตาม currency_unit ของ SO ณ ตอนรับ';

-- =============================================
-- 2. tbt_sale_order_deposit_apply
--    ส่วนของมัดจำที่ถูกหักเข้า invoice แต่ละใบ — 1 มัดจำหักเข้าได้หลาย invoice, 1 invoice รับหักจากหลายมัดจำได้ (FIFO)
--    FK (invoice_running, so_number) -> tbt_sale_invoice_header(running, so_running) เพราะ header ใช้ PK ร่วม (running, so_running) จริง
--    (รูปแบบเดียวกับ tbt_sale_invoice_payment_item ที่ FK ไปยัง header ด้วยคู่ invoice_running+so_running อยู่แล้ว)
-- =============================================
CREATE TABLE IF NOT EXISTS tbt_sale_order_deposit_apply (
    id               BIGSERIAL NOT NULL,
    deposit_running  CHARACTER VARYING NOT NULL,
    so_number        CHARACTER VARYING NOT NULL,
    invoice_running  CHARACTER VARYING NOT NULL,
    amount           NUMERIC NOT NULL,
    is_delete        BOOLEAN NOT NULL DEFAULT false,
    create_date      TIMESTAMPTZ NOT NULL,
    create_by        CHARACTER VARYING NOT NULL,
    update_date      TIMESTAMPTZ,
    update_by        CHARACTER VARYING,
    CONSTRAINT tbt_sale_order_deposit_apply_pk PRIMARY KEY (id),
    CONSTRAINT tbt_sale_order_deposit_apply_amount_ck CHECK (amount > 0),
    CONSTRAINT tbt_sale_order_deposit_apply_deposit_fk FOREIGN KEY (deposit_running)
        REFERENCES tbt_sale_order_deposit (running),
    CONSTRAINT tbt_sale_order_deposit_apply_invoice_fk FOREIGN KEY (invoice_running, so_number)
        REFERENCES tbt_sale_invoice_header (running, so_running)
);

CREATE INDEX IF NOT EXISTS idx_tbt_sale_order_deposit_apply_so_number ON tbt_sale_order_deposit_apply(so_number);
CREATE INDEX IF NOT EXISTS idx_tbt_sale_order_deposit_apply_deposit_running ON tbt_sale_order_deposit_apply(deposit_running);
CREATE INDEX IF NOT EXISTS idx_tbt_sale_order_deposit_apply_invoice_running ON tbt_sale_order_deposit_apply(invoice_running);

COMMENT ON COLUMN tbt_sale_order_deposit_apply.amount IS 'ส่วนของมัดจำ (deposit_running) ที่ถูกหักเข้า invoice_running ใบนี้';

-- =============================================
-- Rollback:
-- DROP INDEX IF EXISTS idx_tbt_sale_order_deposit_apply_invoice_running;
-- DROP INDEX IF EXISTS idx_tbt_sale_order_deposit_apply_deposit_running;
-- DROP INDEX IF EXISTS idx_tbt_sale_order_deposit_apply_so_number;
-- DROP TABLE IF EXISTS tbt_sale_order_deposit_apply;
-- DROP INDEX IF EXISTS idx_tbt_sale_order_deposit_so_number;
-- DROP TABLE IF EXISTS tbt_sale_order_deposit;
-- =============================================
