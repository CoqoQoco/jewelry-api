-- Migration: Add gold (gold type) to tbt_worker_gold_loss_slip_return
-- Date: 2026-06-24
ALTER TABLE tbt_worker_gold_loss_slip_return
    ADD COLUMN IF NOT EXISTS gold CHARACTER VARYING;
