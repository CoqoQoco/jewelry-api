-- =============================================
-- Migration: Add Worker Gold Loss Slip Return
-- Date: 2026-05-21
-- Description: ตารางรับคืนทองต่อสลิป (1 slip → N return items)
--              และเพิ่ม column total_gold_return_amount ใน header
-- =============================================

-- 1. New return items table
CREATE TABLE IF NOT EXISTS tbt_worker_gold_loss_slip_return (
    id              BIGSERIAL NOT NULL,
    slip_id         BIGINT NOT NULL,
    gold_size       CHARACTER VARYING NOT NULL,
    weight          NUMERIC NOT NULL,
    price_per_gram  NUMERIC NOT NULL,
    amount          NUMERIC NOT NULL,
    create_date     TIMESTAMPTZ NOT NULL,
    create_by       CHARACTER VARYING NOT NULL,
    CONSTRAINT tbt_worker_gold_loss_slip_return_pk PRIMARY KEY (id),
    CONSTRAINT tbt_worker_gold_loss_slip_return_slip_fk FOREIGN KEY (slip_id)
        REFERENCES tbt_worker_gold_loss_slip (id)
);

CREATE INDEX IF NOT EXISTS idx_tbt_worker_gold_loss_slip_return_slip_id
    ON tbt_worker_gold_loss_slip_return (slip_id);

-- 2. Add total_gold_return_amount column to header
ALTER TABLE tbt_worker_gold_loss_slip
    ADD COLUMN IF NOT EXISTS total_gold_return_amount NUMERIC;
