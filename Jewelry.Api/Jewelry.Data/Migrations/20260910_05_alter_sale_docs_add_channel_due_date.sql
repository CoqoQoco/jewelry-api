-- =============================================
-- Migration: Add sale_channel_code, sale_person_username, due_date to sale documents
-- Date: 2026-09-10
-- Description: ผูกใบสั่งขาย/ใบแจ้งหนี้กับจุดขาย (tbm_sale_channel) + เก็บ username จริงของผู้ขาย + วันครบกำหนดชำระบนใบแจ้งหนี้
-- Run order: รันหลัง 20260910_03_create_tbm_sale_channel.sql
-- Re-run safety: Idempotent — ADD COLUMN IF NOT EXISTS + เช็ค pg_constraint ก่อนเพิ่ม FK, รันซ้ำได้ไม่ error
-- =============================================

-- =============================================
-- 1. tbt_sale_order
-- =============================================
ALTER TABLE tbt_sale_order
    ADD COLUMN IF NOT EXISTS sale_channel_code VARCHAR(30) NULL,
    ADD COLUMN IF NOT EXISTS sale_person_username VARCHAR(100) NULL;

COMMENT ON COLUMN tbt_sale_order.sale_person_username IS 'username จริงของผู้ขาย ผูกกับ tbm_user — คอลัมน์ sale_person เดิมเก็บชื่อเล่นเป็น free text จับคู่ user ไม่ได้ ให้คงไว้ตามเดิม';

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'tbt_sale_order_sale_channel_fk'
    ) THEN
        ALTER TABLE tbt_sale_order
            ADD CONSTRAINT tbt_sale_order_sale_channel_fk FOREIGN KEY (sale_channel_code)
                REFERENCES tbm_sale_channel(code);
    END IF;
END $$;

-- =============================================
-- 2. tbt_sale_invoice_header
-- =============================================
ALTER TABLE tbt_sale_invoice_header
    ADD COLUMN IF NOT EXISTS sale_channel_code VARCHAR(30) NULL,
    ADD COLUMN IF NOT EXISTS due_date TIMESTAMPTZ NULL,
    ADD COLUMN IF NOT EXISTS sale_person_username VARCHAR(100) NULL;

COMMENT ON COLUMN tbt_sale_invoice_header.due_date IS 'วันครบกำหนดชำระ คำนวณตอนออกบิล — NULL = ใบเก่าให้ถือวันที่ออกบิล (create_date) เป็นฐาน';
COMMENT ON COLUMN tbt_sale_invoice_header.sale_person_username IS 'username จริงของผู้ขาย ผูกกับ tbm_user — สแนปช็อตจากใบสั่งขาย ณ ตอนออกบิล';

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'tbt_sale_invoice_header_sale_channel_fk'
    ) THEN
        ALTER TABLE tbt_sale_invoice_header
            ADD CONSTRAINT tbt_sale_invoice_header_sale_channel_fk FOREIGN KEY (sale_channel_code)
                REFERENCES tbm_sale_channel(code);
    END IF;
END $$;

CREATE INDEX IF NOT EXISTS idx_sale_invoice_header_channel
    ON tbt_sale_invoice_header (sale_channel_code, create_date DESC);
