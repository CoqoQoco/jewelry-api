-- =============================================
-- Migration: Fix Gold Loss slip net_weight_loss sign
-- Date: 2026-10-30
-- Ticket: TK202610300008
-- Description: net_weight_loss เดิมคำนวณเป็น total_weight_loss - น้ำหนักทองคืน (ผิด)
--              ส่วนยอดเงินคำนวณเป็น loss + ยอดทองคืน (ถูก) — ทองที่คืนเป็นคุณกับช่าง เครื่องหมายต้องตรงกับยอดเงิน
--              แก้ใหม่: net_weight_loss = total_weight_loss + SUM(weight ของแถวคืนที่ count_in_calc = true)
--              สำรองค่าเดิมใน tbt_worker_gold_loss_slip_netw_bak ก่อน (สำรองครั้งแรกครั้งเดียว) — รันซ้ำได้ผลเท่าเดิม
-- Rollback: 20261030_04_fix_goldloss_net_weight_sign_rollback.sql
-- =============================================

BEGIN;

-- =============================================
-- 1. Backup (เฉพาะ slip ที่ยังไม่เคยสำรอง — รันซ้ำจะไม่ทับค่าเดิม)
-- =============================================
CREATE TABLE IF NOT EXISTS tbt_worker_gold_loss_slip_netw_bak (
    id bigint PRIMARY KEY,
    net_weight_loss numeric
);

INSERT INTO tbt_worker_gold_loss_slip_netw_bak (id, net_weight_loss)
SELECT s.id, s.net_weight_loss
FROM tbt_worker_gold_loss_slip s
WHERE NOT EXISTS (SELECT 1 FROM tbt_worker_gold_loss_slip_netw_bak b WHERE b.id = s.id);

-- =============================================
-- 2. Recalculate
-- =============================================
UPDATE tbt_worker_gold_loss_slip s
SET net_weight_loss = s.total_weight_loss + COALESCE((
    SELECT SUM(r.weight)
    FROM tbt_worker_gold_loss_slip_return r
    WHERE r.slip_id = s.id AND r.count_in_calc
), 0);

-- ตรวจก่อน COMMIT: จำนวนที่สำรองต้องเท่ากับจำนวน slip — ถ้าไม่ใช่ให้ ROLLBACK
SELECT (SELECT COUNT(*) FROM tbt_worker_gold_loss_slip_netw_bak) AS backed_up_rows,
       (SELECT COUNT(*) FROM tbt_worker_gold_loss_slip) AS slip_rows;

COMMIT;

-- =============================================
-- Verification (รันหลัง COMMIT)
-- =============================================
-- 1) ต้องไม่เหลือแถวที่ไม่ตรงสูตร — expected 0 แถว
-- SELECT s.id, s.document_no, s.total_weight_loss, s.net_weight_loss
-- FROM tbt_worker_gold_loss_slip s
-- WHERE s.net_weight_loss IS DISTINCT FROM s.total_weight_loss + COALESCE((
--     SELECT SUM(r.weight) FROM tbt_worker_gold_loss_slip_return r
--     WHERE r.slip_id = s.id AND r.count_in_calc), 0);

-- 2) เทียบค่าเดิม/ค่าใหม่ของ slip ที่เปลี่ยน (มีทองคืนเท่านั้นที่เปลี่ยน)
-- SELECT s.id, s.document_no, b.net_weight_loss AS old_net, s.net_weight_loss AS new_net
-- FROM tbt_worker_gold_loss_slip s
-- JOIN tbt_worker_gold_loss_slip_netw_bak b ON b.id = s.id
-- WHERE b.net_weight_loss IS DISTINCT FROM s.net_weight_loss
-- ORDER BY s.id;
