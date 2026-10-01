-- =============================================
-- Migration: Gold Loss Target — เพิ่มมิติ scope (SLIP vs STAGE) กันชนกันระหว่างเป้าหมายใบ gold loss กับ
-- เป้าหมายรายแผนก (จ่าย-รับ)
-- Date: 2026-10-01
-- Description: worker_type=80 ใช้ซ้ำกันระหว่าง "ช่างฝัง" (SLIP, ของเดิม) กับ "แผนกฝัง" (STAGE, ใหม่) — ต้องแยก
--   ด้วย scope ไม่งั้นชนกัน — ตาราง tbt_production_gold_loss_target ยังเป็น append-only history เหมือนเดิม
--   แค่เพิ่มมิติ key จาก (worker_type, metal) เป็น (scope, worker_type, metal)
-- แก้ไข (รอบ 2): migration เดิมพังบน prod ทั้งไฟล์ (rollback หมด ไม่มีอะไรถูก apply) เพราะ constraint เดิม
--   tbt_production_gold_loss_target_type_ck = CHECK (worker_type IN (50, 80)) จากตาราง 20261001_01 ยังอยู่
--   และปฏิเสธแถว STAGE ที่ worker_type=60/90 ตอน seed — ต้อง DROP constraint เดิมทิ้งก่อน แล้วแทนที่ด้วย
--   constraint ใหม่ที่ scope-aware (SLIP ยังจำกัดแค่ 50/80 เหมือนเดิม, STAGE อนุญาต 60/80/90)
-- Re-run safety: Idempotent — IF NOT EXISTS / WHERE NOT EXISTS / DROP CONSTRAINT IF EXISTS ก่อน ADD ทุกจุด
-- =============================================

ALTER TABLE tbt_production_gold_loss_target
    ADD COLUMN IF NOT EXISTS scope CHARACTER VARYING NOT NULL DEFAULT 'SLIP';

ALTER TABLE tbt_production_gold_loss_target
    DROP CONSTRAINT IF EXISTS tbt_production_gold_loss_target_scope_ck;
ALTER TABLE tbt_production_gold_loss_target
    ADD CONSTRAINT tbt_production_gold_loss_target_scope_ck CHECK (scope IN ('SLIP', 'STAGE'));

-- constraint เดิมจาก 20261001_01 ตรวจแค่ worker_type IN (50,80) — บล็อกแถว STAGE (60/90) ต้อง DROP ทิ้งก่อน
ALTER TABLE tbt_production_gold_loss_target
    DROP CONSTRAINT IF EXISTS tbt_production_gold_loss_target_type_ck;

-- แทนที่ด้วย constraint ใหม่ scope-aware: SLIP จำกัดแค่ 50 (ช่างแต่ง) / 80 (ช่างฝัง) เหมือนเดิม,
-- STAGE อนุญาต 60 (rawPolish) / 80 (setting) / 90 (plating)
ALTER TABLE tbt_production_gold_loss_target
    DROP CONSTRAINT IF EXISTS tbt_production_gold_loss_target_scope_type_ck;
ALTER TABLE tbt_production_gold_loss_target
    ADD CONSTRAINT tbt_production_gold_loss_target_scope_type_ck
    CHECK ((scope = 'SLIP' AND worker_type IN (50, 80)) OR (scope = 'STAGE' AND worker_type IN (60, 80, 90)));

DROP INDEX IF EXISTS idx_tbt_production_gold_loss_target_type_metal_effective;
CREATE INDEX IF NOT EXISTS idx_tbt_production_gold_loss_target_scope_type_metal_effective
    ON tbt_production_gold_loss_target (scope, worker_type, metal, effective_from DESC);

-- แถวเดิมทั้งหมด (ก่อน migration นี้) เป็น scope='SLIP' โดย default อยู่แล้ว (ADD COLUMN DEFAULT) — ไม่ต้อง UPDATE อะไร

-- Seed เป้าหมายรายแผนก (STAGE) — worker_type ในที่นี้คือ "สถานะทำงาน" ของแผนก (60=ขัดมัน/rawPolish,
-- 80=ฝัง/setting, 90=ชุบ/plating) ไม่ใช่ worker_type ของใบ gold loss — trim(50)/gemSort(70) ไม่มีเป้าหมาย STAGE
-- (trim มีเศษทองหล่อปนในผลต่าง, gemSort ไม่ได้ชั่งน้ำหนักเลย) — idempotent: ข้ามถ้ามีแถว (scope,worker_type,metal) นั้นแล้ว
INSERT INTO tbt_production_gold_loss_target (scope, worker_type, metal, target_percent, effective_from, remark, create_date, create_by)
SELECT 'STAGE', t.worker_type, t.metal, t.target_percent, NOW(), 'ค่าเริ่มต้น (รายแผนก จ่าย-รับ)', NOW(), 'system'
FROM (VALUES
    (60, 'GOLD', 3.2),
    (60, 'SILVER', 3.2),
    (80, 'GOLD', 4.3),
    (80, 'SILVER', 2.4),
    (90, 'GOLD', 2.1),
    (90, 'SILVER', 4.7)
) AS t(worker_type, metal, target_percent)
WHERE NOT EXISTS (
    SELECT 1 FROM tbt_production_gold_loss_target g
    WHERE g.scope = 'STAGE' AND g.worker_type = t.worker_type AND g.metal = t.metal
);

-- =============================================
-- Verification (uncomment to run manually)
-- =============================================
-- SELECT scope, worker_type, metal, target_percent, effective_from, create_by, remark
-- FROM tbt_production_gold_loss_target
-- ORDER BY scope, worker_type, metal, effective_from DESC;
--
-- SELECT conname, pg_get_constraintdef(oid) FROM pg_constraint
-- WHERE conrelid = 'tbt_production_gold_loss_target'::regclass;
