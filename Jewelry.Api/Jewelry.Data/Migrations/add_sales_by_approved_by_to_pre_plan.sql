-- Migration: Add sales_by and approved_by to tbt_production_pre_plan
-- Date: 2026-05-20
-- Description: เพิ่ม field ผู้สั่งผลิตงานขาย (sales_by) และผู้อนุมัติ (approved_by) ใน pre-plan header
ALTER TABLE tbt_production_pre_plan
    ADD COLUMN IF NOT EXISTS sales_by CHARACTER VARYING,
    ADD COLUMN IF NOT EXISTS approved_by CHARACTER VARYING;
