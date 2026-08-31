-- =============================================
-- Migration: POS Checkout idempotency
-- Date: 2026-08-29
-- Description: เก็บผลลัพธ์ของ POS/Checkout ต่อ idempotency key
--              กันสร้าง SO/Invoice/Payment ซ้ำเมื่อ client retry (เน็ตหลุดที่งานแฟร์)
-- =============================================
CREATE TABLE IF NOT EXISTS tbt_pos_checkout (
    idempotency_key CHARACTER VARYING NOT NULL,
    so_number       CHARACTER VARYING NOT NULL,
    invoice_number  CHARACTER VARYING NOT NULL,
    grand_total     NUMERIC NOT NULL DEFAULT 0,
    paid_amount     NUMERIC NOT NULL DEFAULT 0,
    create_date     TIMESTAMPTZ NOT NULL,
    create_by       CHARACTER VARYING NOT NULL,
    CONSTRAINT tbt_pos_checkout_pk PRIMARY KEY (idempotency_key)
);

CREATE INDEX IF NOT EXISTS idx_pos_checkout_so_number ON tbt_pos_checkout(so_number);
CREATE INDEX IF NOT EXISTS idx_pos_checkout_invoice_number ON tbt_pos_checkout(invoice_number);
