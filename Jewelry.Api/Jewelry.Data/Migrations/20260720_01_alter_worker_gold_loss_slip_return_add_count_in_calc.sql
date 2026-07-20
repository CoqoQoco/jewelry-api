-- Migration: Add count_in_calc to tbt_worker_gold_loss_slip_return
-- Date: 2026-07-20
-- Description: flag ต่อบรรทัดรายการคืนทอง — false = ไม่นำน้ำหนัก/ยอดมาคำนวณยอดสุทธิ
ALTER TABLE tbt_worker_gold_loss_slip_return
    ADD COLUMN IF NOT EXISTS count_in_calc BOOLEAN NOT NULL DEFAULT true;
