-- =============================================
-- Migration: Worker Gold Loss Slip
-- Date: 2026-05-20
-- Description: สร้าง slip header + item สำหรับบันทึก gold loss ต่อช่าง
-- =============================================

-- =============================================
-- 0. Stamp column on existing detail table
-- =============================================
ALTER TABLE tbt_production_plan_status_detail
    ADD COLUMN IF NOT EXISTS worker_gold_loss_slip_id BIGINT;

CREATE INDEX IF NOT EXISTS idx_tbt_production_plan_status_detail_gls
    ON tbt_production_plan_status_detail(worker_gold_loss_slip_id);

-- =============================================
-- 1. Header table
-- =============================================
CREATE TABLE IF NOT EXISTS tbt_worker_gold_loss_slip (
    id                  BIGSERIAL NOT NULL,
    document_no         CHARACTER VARYING NOT NULL,
    worker_code         CHARACTER VARYING NOT NULL,
    worker_name         CHARACTER VARYING,
    request_date_start  TIMESTAMPTZ NOT NULL,
    request_date_end    TIMESTAMPTZ NOT NULL,
    gold_return         NUMERIC,
    total_weight_loss   NUMERIC,
    net_weight_loss     NUMERIC,
    total_money_diff    NUMERIC,
    remark              CHARACTER VARYING,
    is_active           BOOLEAN NOT NULL DEFAULT true,
    create_date         TIMESTAMPTZ NOT NULL,
    create_by           CHARACTER VARYING NOT NULL,
    update_date         TIMESTAMPTZ,
    update_by           CHARACTER VARYING,
    CONSTRAINT tbt_worker_gold_loss_slip_pk PRIMARY KEY (id),
    CONSTRAINT tbt_worker_gold_loss_slip_doc_uq UNIQUE (document_no)
);

CREATE INDEX IF NOT EXISTS idx_tbt_worker_gold_loss_slip_worker ON tbt_worker_gold_loss_slip(worker_code);
CREATE INDEX IF NOT EXISTS idx_tbt_worker_gold_loss_slip_date ON tbt_worker_gold_loss_slip(request_date_start, request_date_end);

-- =============================================
-- 2. Item table (snapshot)
-- =============================================
CREATE TABLE IF NOT EXISTS tbt_worker_gold_loss_slip_item (
    id                    BIGSERIAL NOT NULL,
    slip_id               BIGINT NOT NULL,
    wo                    CHARACTER VARYING,
    wo_number             INT,
    product_number        CHARACTER VARYING,
    product_name          CHARACTER VARYING,
    gold                  CHARACTER VARYING,
    gold_size             CHARACTER VARYING,
    job_date              TIMESTAMPTZ,
    gold_qty_send         NUMERIC,
    gold_weight_send      NUMERIC,
    gold_qty_check        NUMERIC,
    gold_weight_check     NUMERIC,
    loss_percent          NUMERIC,
    weight_loss_allowed   NUMERIC,
    weight_loss_actual    NUMERIC,
    gold_loss_price       NUMERIC,
    money_diff            NUMERIC,
    is_active             BOOLEAN NOT NULL DEFAULT true,
    CONSTRAINT tbt_worker_gold_loss_slip_item_pk PRIMARY KEY (id),
    CONSTRAINT tbt_worker_gold_loss_slip_item_slip_fk
        FOREIGN KEY (slip_id) REFERENCES tbt_worker_gold_loss_slip(id)
);

CREATE INDEX IF NOT EXISTS idx_tbt_worker_gold_loss_slip_item_slip ON tbt_worker_gold_loss_slip_item(slip_id);
