-- =============================================
-- Migration: Pre-plan material เพิ่มไซส์แหวนระดับแถววัตถุดิบ (TK202611060001)
-- Date: 2026-11-06
-- Description: เพิ่มคอลัมน์ ring_size (NULLABLE) ใน tbt_production_pre_plan_material
-- Re-run safety: Idempotent — ADD COLUMN IF NOT EXISTS
-- =============================================

ALTER TABLE tbt_production_pre_plan_material
    ADD COLUMN IF NOT EXISTS ring_size CHARACTER VARYING(50);
