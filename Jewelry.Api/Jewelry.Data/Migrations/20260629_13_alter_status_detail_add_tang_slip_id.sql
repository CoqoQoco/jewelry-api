-- =============================================
-- Migration: Add gold_loss_tang_slip_id to tbt_production_plan_status_detail
-- Date: 2026-06-29
-- Description: เพิ่ม column gold_loss_tang_slip_id เพื่อ stamp งานที่ลงใบ Tang slip แล้ว
--              (เลียนแบบ worker_gold_loss_slip_id ของ stage 80)
-- =============================================

ALTER TABLE tbt_production_plan_status_detail
    ADD COLUMN IF NOT EXISTS gold_loss_tang_slip_id BIGINT;

CREATE INDEX IF NOT EXISTS idx_status_detail_gold_loss_tang_slip_id
    ON tbt_production_plan_status_detail (gold_loss_tang_slip_id);
