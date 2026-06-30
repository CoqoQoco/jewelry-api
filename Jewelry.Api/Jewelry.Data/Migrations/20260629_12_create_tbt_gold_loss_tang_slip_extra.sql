-- =============================================
-- Migration: Gold Loss Tang Slip Extra Lines
-- Date: 2026-06-29
-- Description: สร้างตาราง tbt_gold_loss_tang_slip_extra รายการเบิก/คืนที่ผู้ใช้เพิ่มเอง
-- =============================================

CREATE TABLE IF NOT EXISTS tbt_gold_loss_tang_slip_extra (
    id        BIGSERIAL NOT NULL,
    slip_id   BIGINT NOT NULL,
    kind      INT NOT NULL,
    -- kind: 1=issued/เบิก, 2=returned/คืน
    name      CHARACTER VARYING,
    weight    NUMERIC,
    is_active BOOLEAN NOT NULL DEFAULT true,
    CONSTRAINT tbt_gold_loss_tang_slip_extra_pk PRIMARY KEY (id),
    CONSTRAINT tbt_gold_loss_tang_slip_extra_slip_fk FOREIGN KEY (slip_id)
        REFERENCES tbt_gold_loss_tang_slip (id)
);

CREATE INDEX IF NOT EXISTS idx_gold_loss_tang_slip_extra_slip_id ON tbt_gold_loss_tang_slip_extra (slip_id);
