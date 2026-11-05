-- =============================================
-- Migration: Pre-plan item เพิ่มไซส์แหวน (TK202611050004)
-- Date: 2026-11-05
-- Description: เพิ่มคอลัมน์ ring_size (NULLABLE) ใน tbt_production_pre_plan_item
-- Re-run safety: Idempotent — ADD COLUMN IF NOT EXISTS
-- =============================================

ALTER TABLE tbt_production_pre_plan_item
    ADD COLUMN IF NOT EXISTS ring_size CHARACTER VARYING(50);
