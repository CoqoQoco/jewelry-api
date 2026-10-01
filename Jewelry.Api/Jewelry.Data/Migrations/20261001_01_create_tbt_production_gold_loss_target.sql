-- =============================================
-- Migration: Gold Loss Target (เป้าหมาย % เสียทองต่อประเภทช่าง) สำหรับหน้า ProductionInsight "ทองและ Loss"
-- Date: 2026-10-01
-- Description: ตาราง append-only history เก็บ "เป้าหมาย % เสียทองของบริษัท" แยกต่อประเภทช่าง
--   (50=ช่างแต่ง, 80=ช่างฝัง) — ทุกครั้งที่บันทึกค่าใหม่ = insert แถวใหม่ (ไม่ update ของเดิม)
--   ค่าที่ใช้งานจริง ณ เวลาใดๆ = แถวล่าสุดของแต่ละ worker_type ที่ effective_from <= เวลานั้น
--   สิทธิ์แก้ไข reuse permission เดิม production:standard-edit (ไม่สร้าง permission ใหม่)
-- Re-run safety: Idempotent — IF NOT EXISTS / WHERE NOT EXISTS ทั้งหมด
-- =============================================

CREATE TABLE IF NOT EXISTS tbt_production_gold_loss_target (
    id              BIGSERIAL,
    worker_type     INT NOT NULL,
    -- worker_type: 50 = ช่างแต่ง (trim), 80 = ช่างฝัง (setting)
    target_percent  NUMERIC(6,3) NOT NULL,
    effective_from  TIMESTAMPTZ NOT NULL,
    remark          CHARACTER VARYING,
    create_date     TIMESTAMPTZ NOT NULL,
    create_by       CHARACTER VARYING NOT NULL,
    CONSTRAINT tbt_production_gold_loss_target_pk PRIMARY KEY (id),
    CONSTRAINT tbt_production_gold_loss_target_type_ck CHECK (worker_type IN (50, 80)),
    CONSTRAINT tbt_production_gold_loss_target_percent_ck CHECK (target_percent >= 0 AND target_percent <= 100)
);

CREATE INDEX IF NOT EXISTS idx_tbt_production_gold_loss_target_type_effective
    ON tbt_production_gold_loss_target (worker_type, effective_from DESC);

-- Seed ค่าเริ่มต้น: 2.3% ช่างแต่ง(50), 2.4% ช่างฝัง(80) — ข้ามถ้า worker_type นั้นมีแถวอยู่แล้ว (idempotent)
INSERT INTO tbt_production_gold_loss_target (worker_type, target_percent, effective_from, remark, create_date, create_by)
SELECT t.worker_type, t.target_percent, NOW(), 'ค่าเริ่มต้น (เฉลี่ยถ่วงน้ำหนัก % allowance ปัจจุบัน)', NOW(), 'system'
FROM (VALUES (50, 2.3), (80, 2.4)) AS t(worker_type, target_percent)
WHERE NOT EXISTS (
    SELECT 1 FROM tbt_production_gold_loss_target g WHERE g.worker_type = t.worker_type
);

-- =============================================
-- Verification (uncomment to run manually)
-- =============================================
-- SELECT worker_type, target_percent, effective_from, create_by, remark
-- FROM tbt_production_gold_loss_target
-- ORDER BY worker_type, effective_from DESC;
