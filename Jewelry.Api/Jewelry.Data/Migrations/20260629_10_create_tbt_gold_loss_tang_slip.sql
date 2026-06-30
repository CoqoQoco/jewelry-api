-- =============================================
-- Migration: Gold Loss Tang Slip Header
-- Date: 2026-06-29
-- Description: สร้างตาราง tbt_gold_loss_tang_slip สำหรับใบ Gold Loss ช่างแต่ง (stage 50)
-- =============================================

CREATE TABLE IF NOT EXISTS tbt_gold_loss_tang_slip (
    id                   BIGSERIAL NOT NULL,
    document_no          CHARACTER VARYING NOT NULL,
    worker_code          CHARACTER VARYING NOT NULL,
    worker_name          CHARACTER VARYING,
    request_date_start   TIMESTAMPTZ,
    request_date_end     TIMESTAMPTZ,
    loss_percent         NUMERIC,
    price_per_gram       NUMERIC,
    issued_total         NUMERIC,
    returned_total       NUMERIC,
    raw_loss             NUMERIC,
    allowed_loss         NUMERIC,
    diff_loss            NUMERIC,
    total_money_diff     NUMERIC,
    remark               CHARACTER VARYING,
    is_active            BOOLEAN NOT NULL DEFAULT true,
    create_date          TIMESTAMPTZ NOT NULL,
    create_by            CHARACTER VARYING NOT NULL,
    update_date          TIMESTAMPTZ,
    update_by            CHARACTER VARYING,
    CONSTRAINT tbt_gold_loss_tang_slip_pk PRIMARY KEY (id),
    CONSTRAINT tbt_gold_loss_tang_slip_document_no_uq UNIQUE (document_no)
);

COMMENT ON COLUMN tbt_gold_loss_tang_slip.loss_percent IS 'เปอร์เซ็นต์สูญหายทองที่อนุญาต';
COMMENT ON COLUMN tbt_gold_loss_tang_slip.price_per_gram IS 'ราคาทองต่อกรัม สำหรับคำนวณเงิน';
COMMENT ON COLUMN tbt_gold_loss_tang_slip.issued_total IS 'น้ำหนักเบิกรวมทั้งหมด (งาน + รายการเพิ่มเอง)';
COMMENT ON COLUMN tbt_gold_loss_tang_slip.returned_total IS 'น้ำหนักคืนรวมทั้งหมด (งาน + รายการเพิ่มเอง)';
COMMENT ON COLUMN tbt_gold_loss_tang_slip.raw_loss IS 'issued_total - returned_total';
COMMENT ON COLUMN tbt_gold_loss_tang_slip.allowed_loss IS 'returned_total * loss_percent / 100 (ปัด ToPositiveInfinity 4 ตำแหน่ง)';
COMMENT ON COLUMN tbt_gold_loss_tang_slip.diff_loss IS 'allowed_loss - raw_loss (+ = ได้เงิน, - = ขาดเงิน)';
COMMENT ON COLUMN tbt_gold_loss_tang_slip.total_money_diff IS 'diff_loss * price_per_gram (ปัด 2 ตำแหน่ง)';
