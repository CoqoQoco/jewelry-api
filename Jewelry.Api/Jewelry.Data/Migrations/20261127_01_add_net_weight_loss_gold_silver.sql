-- =============================================
-- Migration: แยก net_weight_loss ของใบสรุป Gold Loss (GLS) เป็นทอง/เงิน
-- Date: 2026-11-27
-- Ticket: TK202611260001
-- Description: เพิ่ม net_weight_loss_gold / net_weight_loss_silver ใน tbt_worker_gold_loss_slip
--              backfill ทุกใบจาก item.weight_loss_actual + return.weight (count_in_calc = true)
--              เงิน = gold = 'SV' หรือ lower(gold_size) = 'silver' (ตรงกับ PurityKey ใน C#) · ที่เหลือ = ทอง
--              โลหะที่ไม่มีแถวเลย (ทั้ง item และ return) = NULL (ไม่ใช่ 0)
-- ลำดับ deploy: รันก่อน deploy API (API ใหม่เขียน/อ่านคอลัมน์นี้) · รันซ้ำได้ผลเท่าเดิม
-- Rollback: 20261127_01_add_net_weight_loss_gold_silver_rollback.sql
-- =============================================

BEGIN;

ALTER TABLE tbt_worker_gold_loss_slip
    ADD COLUMN IF NOT EXISTS net_weight_loss_gold numeric NULL,
    ADD COLUMN IF NOT EXISTS net_weight_loss_silver numeric NULL;

WITH lines AS (
    SELECT i.slip_id,
           COALESCE(i.gold = 'SV' OR lower(i.gold_size) = 'silver', false) AS is_silver,
           COALESCE(i.weight_loss_actual, 0) AS w
    FROM tbt_worker_gold_loss_slip_item i
    UNION ALL
    SELECT r.slip_id,
           COALESCE(r.gold = 'SV' OR lower(r.gold_size) = 'silver', false) AS is_silver,
           COALESCE(r.weight, 0) AS w
    FROM tbt_worker_gold_loss_slip_return r
    WHERE r.count_in_calc = true
),
agg AS (
    SELECT slip_id,
           SUM(w) FILTER (WHERE NOT is_silver) AS gold_w,
           SUM(w) FILTER (WHERE is_silver)     AS silver_w
    FROM lines
    GROUP BY slip_id
)
UPDATE tbt_worker_gold_loss_slip s
SET net_weight_loss_gold   = a.gold_w,
    net_weight_loss_silver = a.silver_w
FROM agg a
WHERE a.slip_id = s.id;

-- ใบที่ไม่มีแถวเลย (ไม่อยู่ใน agg) ให้เป็น NULL ทั้งคู่
UPDATE tbt_worker_gold_loss_slip s
SET net_weight_loss_gold = NULL,
    net_weight_loss_silver = NULL
WHERE NOT EXISTS (SELECT 1 FROM tbt_worker_gold_loss_slip_item i WHERE i.slip_id = s.id)
  AND NOT EXISTS (SELECT 1 FROM tbt_worker_gold_loss_slip_return r WHERE r.slip_id = s.id AND r.count_in_calc = true);

COMMIT;

-- =============================================
-- Verification (รันหลัง COMMIT) — expected 0 แถว
-- =============================================
-- SELECT s.id, s.document_no, s.net_weight_loss, s.net_weight_loss_gold, s.net_weight_loss_silver
-- FROM tbt_worker_gold_loss_slip s
-- WHERE round(COALESCE(s.net_weight_loss_gold, 0) + COALESCE(s.net_weight_loss_silver, 0), 4)
--       IS DISTINCT FROM round(COALESCE(s.net_weight_loss, 0), 4);
