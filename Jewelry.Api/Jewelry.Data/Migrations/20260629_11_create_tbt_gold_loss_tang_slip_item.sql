-- =============================================
-- Migration: Gold Loss Tang Slip Item
-- Date: 2026-06-29
-- Description: สร้างตาราง tbt_gold_loss_tang_slip_item snapshot งานที่เลือกในใบ tang slip
-- =============================================

CREATE TABLE IF NOT EXISTS tbt_gold_loss_tang_slip_item (
    id                   BIGSERIAL NOT NULL,
    slip_id              BIGINT NOT NULL,
    production_plan_id   INT,
    item_no              CHARACTER VARYING,
    wo                   CHARACTER VARYING,
    wo_number            INT,
    product_number       CHARACTER VARYING,
    product_name         CHARACTER VARYING,
    gold                 CHARACTER VARYING,
    gold_size            CHARACTER VARYING,
    job_date             TIMESTAMPTZ,
    gold_qty_send        NUMERIC,
    gold_weight_send     NUMERIC,
    gold_qty_check       NUMERIC,
    gold_weight_check    NUMERIC,
    is_active            BOOLEAN NOT NULL DEFAULT true,
    CONSTRAINT tbt_gold_loss_tang_slip_item_pk PRIMARY KEY (id),
    CONSTRAINT tbt_gold_loss_tang_slip_item_slip_fk FOREIGN KEY (slip_id)
        REFERENCES tbt_gold_loss_tang_slip (id)
);

CREATE INDEX IF NOT EXISTS idx_gold_loss_tang_slip_item_slip_id ON tbt_gold_loss_tang_slip_item (slip_id);
