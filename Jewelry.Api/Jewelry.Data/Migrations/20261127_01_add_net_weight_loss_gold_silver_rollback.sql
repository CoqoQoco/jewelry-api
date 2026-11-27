-- =============================================
-- Rollback: 20261127_01_add_net_weight_loss_gold_silver.sql
-- Ticket: TK202611260001
-- ลำดับ: ถอย UI -> ถอย API ก่อน แล้วค่อยรันไฟล์นี้ (API เก่าไม่อ้างคอลัมน์นี้ แต่ API ใหม่พังถ้าคอลัมน์หาย)
-- =============================================

BEGIN;

ALTER TABLE tbt_worker_gold_loss_slip
    DROP COLUMN IF EXISTS net_weight_loss_gold,
    DROP COLUMN IF EXISTS net_weight_loss_silver;

COMMIT;
