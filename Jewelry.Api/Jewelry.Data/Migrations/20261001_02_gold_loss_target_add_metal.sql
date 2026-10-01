-- =============================================
-- Migration: Gold Loss Target — เพิ่มมิติโลหะ (ทอง/เงิน) ให้เป้าหมาย % เสียทอง
-- Date: 2026-10-01
-- Description: ใบ gold loss ปนกันระหว่างทองกับเงิน (ราคาต่างกันมาก 40 บาท/ก. เงิน vs ~1,150-2,300 บาท/ก. ทอง)
--   เป้าหมาย % ต้องแยกต่อ (worker_type, metal) ไม่ใช่แค่ worker_type เฉยๆ
-- Re-run safety: Idempotent — IF NOT EXISTS / WHERE NOT EXISTS / เงื่อนไข guard ก่อน UPDATE ทั้งหมด
-- =============================================

ALTER TABLE tbt_production_gold_loss_target
    ADD COLUMN IF NOT EXISTS metal CHARACTER VARYING NOT NULL DEFAULT 'GOLD';

ALTER TABLE tbt_production_gold_loss_target
    DROP CONSTRAINT IF EXISTS tbt_production_gold_loss_target_metal_ck;
ALTER TABLE tbt_production_gold_loss_target
    ADD CONSTRAINT tbt_production_gold_loss_target_metal_ck CHECK (metal IN ('GOLD', 'SILVER'));

DROP INDEX IF EXISTS idx_tbt_production_gold_loss_target_type_effective;
CREATE INDEX IF NOT EXISTS idx_tbt_production_gold_loss_target_type_metal_effective
    ON tbt_production_gold_loss_target (worker_type, metal, effective_from DESC);

-- ปรับ 2 แถว seed เดิม (migration 20261001_01) เป็นค่าเฉลี่ย allowance ทองที่วัดได้จริงจาก prod —
-- ทำเฉพาะตอนที่ยังเป็น seed ดั้งเดิมที่ไม่มีใครแก้ (create_by='system' และยังมีแถวเดียวของ worker_type นั้น
-- ไม่เช่นนั้นแปลว่ามีคนบันทึกค่าใหม่ผ่าน SaveGoldLossTargets ไปแล้ว ไม่ควรทับ)
UPDATE tbt_production_gold_loss_target
SET target_percent = 2.2,
    remark = 'ปรับตามค่า allowance เฉลี่ยถ่วงน้ำหนักที่วัดได้จริง (ทอง)'
WHERE worker_type = 50
  AND metal = 'GOLD'
  AND create_by = 'system'
  AND (SELECT COUNT(*) FROM tbt_production_gold_loss_target t2 WHERE t2.worker_type = 50) = 1;

UPDATE tbt_production_gold_loss_target
SET target_percent = 3.4,
    remark = 'ปรับตามค่า allowance เฉลี่ยถ่วงน้ำหนักที่วัดได้จริง (ทอง)'
WHERE worker_type = 80
  AND metal = 'GOLD'
  AND create_by = 'system'
  AND (SELECT COUNT(*) FROM tbt_production_gold_loss_target t2 WHERE t2.worker_type = 80) = 1;

-- Seed เป้าหมายเงิน (SILVER) — ข้ามถ้ามีแถวของ (worker_type, 'SILVER') อยู่แล้ว (idempotent)
INSERT INTO tbt_production_gold_loss_target (worker_type, metal, target_percent, effective_from, remark, create_date, create_by)
SELECT t.worker_type, 'SILVER', t.target_percent, NOW(), 'ค่าเริ่มต้น (เงิน)', NOW(), 'system'
FROM (VALUES (50, 2.5), (80, 2.3)) AS t(worker_type, target_percent)
WHERE NOT EXISTS (
    SELECT 1 FROM tbt_production_gold_loss_target g WHERE g.worker_type = t.worker_type AND g.metal = 'SILVER'
);

-- =============================================
-- Verification (uncomment to run manually)
-- =============================================
-- SELECT worker_type, metal, target_percent, effective_from, create_by, remark
-- FROM tbt_production_gold_loss_target
-- ORDER BY worker_type, metal, effective_from DESC;
