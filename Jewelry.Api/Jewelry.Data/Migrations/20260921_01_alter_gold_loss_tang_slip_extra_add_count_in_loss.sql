-- Migration: Add count_in_loss to tbt_gold_loss_tang_slip_extra
-- Date: 2026-09-21
-- Description: flag ต่อบรรทัดรายการคืน (kind=2) — true = นำน้ำหนักรายการนี้มารวมในฐานคำนวณ %Loss
-- (น้ำหนักคืนที่นำมาคิด %Loss เช่น งานซ่อม/งานส่งที่ไม่มี Job) default false ไม่กระทบใบเดิม
ALTER TABLE tbt_gold_loss_tang_slip_extra
    ADD COLUMN IF NOT EXISTS count_in_loss BOOLEAN NOT NULL DEFAULT false;

COMMENT ON COLUMN tbt_gold_loss_tang_slip_extra.count_in_loss IS
    'true = นำน้ำหนักรายการคืนนี้ไปรวมในฐานคำนวณ %Loss (เช่น งานซ่อม/งานส่งที่ไม่มี Job) — มีผลเฉพาะรายการคืน (kind=2), default false ไม่กระทบใบเดิม';
