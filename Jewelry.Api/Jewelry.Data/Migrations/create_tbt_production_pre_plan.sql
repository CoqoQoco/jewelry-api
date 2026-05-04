-- =============================================
-- Migration: Production Pre-Plan
-- Date: 2026-05-03
-- Description: สร้าง table ใบสั่งผลิต (Pre-Plan) สำหรับแผนกแม่พิมพ์
-- =============================================

-- =============================================
-- 1. tbt_production_pre_plan
-- =============================================
CREATE TABLE IF NOT EXISTS tbt_production_pre_plan (
    id                      SERIAL,
    order_no                CHARACTER VARYING(50) NOT NULL,
    job_location            CHARACTER VARYING(20) NOT NULL,
    -- job_location: 'Domestic' / 'Overseas'
    job_type                CHARACTER VARYING(30) NOT NULL,
    -- job_type: 'NewDesign' / 'Sale' / 'CustomCustomer'
    production_round        INTEGER NOT NULL DEFAULT 1,
    mold_code               CHARACTER VARYING(50) NOT NULL,
    product_type            CHARACTER VARYING(100),
    gold_type               CHARACTER VARYING(10) NOT NULL,
    -- gold_type: '18K' / '14K' / '9K' / 'Silver'
    mold_detail             TEXT,
    remark                  TEXT,
    order_date              TIMESTAMPTZ NOT NULL,
    delivery_date           TIMESTAMPTZ NOT NULL,
    status                  CHARACTER VARYING(20) NOT NULL DEFAULT 'Draft',
    -- status: 'Draft' / 'Submitted' / 'Approved' / 'Rejected' / 'Consumed'
    product_qty             INTEGER,
    reject_reason           TEXT,
    linked_production_plan_id INTEGER,
    create_by               CHARACTER VARYING(100) NOT NULL,
    create_date             TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    submit_by               CHARACTER VARYING(100),
    submit_date             TIMESTAMPTZ,
    approve_by              CHARACTER VARYING(100),
    approve_date            TIMESTAMPTZ,
    update_by               CHARACTER VARYING(100),
    update_date             TIMESTAMPTZ,
    CONSTRAINT tbt_production_pre_plan_pk PRIMARY KEY (id)
);

CREATE INDEX IF NOT EXISTS idx_tbt_production_pre_plan_status ON tbt_production_pre_plan(status);
CREATE INDEX IF NOT EXISTS idx_tbt_production_pre_plan_mold_code ON tbt_production_pre_plan(mold_code);
CREATE INDEX IF NOT EXISTS idx_tbt_production_pre_plan_linked_production_plan_id ON tbt_production_pre_plan(linked_production_plan_id);
