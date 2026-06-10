-- Migration: Add cancel columns to pre-plan tables
-- Date: 2026-06-10
ALTER TABLE tbt_production_pre_plan_item
    ADD COLUMN IF NOT EXISTS is_cancelled   BOOLEAN DEFAULT FALSE,
    ADD COLUMN IF NOT EXISTS cancel_by      CHARACTER VARYING,
    ADD COLUMN IF NOT EXISTS cancel_date    TIMESTAMPTZ,
    ADD COLUMN IF NOT EXISTS cancel_reason  CHARACTER VARYING;

ALTER TABLE tbt_production_pre_plan
    ADD COLUMN IF NOT EXISTS cancel_by      CHARACTER VARYING,
    ADD COLUMN IF NOT EXISTS cancel_date    TIMESTAMPTZ,
    ADD COLUMN IF NOT EXISTS cancel_reason  CHARACTER VARYING;
