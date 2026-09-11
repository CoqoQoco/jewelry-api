-- =============================================
-- Migration: Add production_plan_id to tbt_worker_gold_loss_slip_item
-- Date: 2026-09-07
-- Description: ผูกรายการใบ gold loss ของช่างฝัง (worker) กลับเข้า production plan
--              (เลียนแบบ tbt_gold_loss_tang_slip_item.production_plan_id ของฝั่งช่างแต่ง)
--              หมายเหตุ: tbt_gold_loss_tang_slip_item ไม่มี index เฉพาะบน production_plan_id
--              (มีแค่ index บน slip_id) จึงไม่เพิ่ม index ให้คอลัมน์นี้เช่นกัน
-- =============================================

ALTER TABLE tbt_worker_gold_loss_slip_item
    ADD COLUMN IF NOT EXISTS production_plan_id INT;
