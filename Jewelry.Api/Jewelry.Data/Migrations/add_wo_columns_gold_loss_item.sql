-- Migration: เพิ่ม wo_number, wo_text ใน tbt_gold_loss_item
-- วันที่: 2026-04-06

ALTER TABLE tbt_gold_loss_item
  ADD COLUMN IF NOT EXISTS wo_number INT,
  ADD COLUMN IF NOT EXISTS wo_text CHARACTER VARYING;
