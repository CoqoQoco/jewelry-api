-- Migration: Gold Loss columns
-- วันที่: 2026-03-09
-- เพิ่ม column สำหรับเก็บ gold loss data ใน status header และ detail

ALTER TABLE tbt_production_plan_status_header
  ADD COLUMN IF NOT EXISTS gold_loss_price NUMERIC(18,4) NULL;

ALTER TABLE tbt_production_plan_status_detail
  ADD COLUMN IF NOT EXISTS loss_percent NUMERIC(10,4) NULL;

ALTER TABLE tbt_production_plan_status_detail
  ADD COLUMN IF NOT EXISTS loss_remark VARCHAR(500) NULL;
