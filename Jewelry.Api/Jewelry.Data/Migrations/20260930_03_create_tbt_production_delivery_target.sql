-- =============================================
-- Migration: Delivery Target (เป้าหมาย % ส่งตรงเวลา) สำหรับหน้า ProductionInsight "ส่งงานตรงเวลา"
-- Date: 2026-09-30
-- Description: ตาราง append-only history เก็บ "เป้าหมาย % ส่งตรงเวลา" — ทุกครั้งที่บันทึกค่าใหม่ = insert แถวใหม่
--   ค่าที่ใช้งานจริง ณ เวลาใดๆ = แถวล่าสุด (effective_from มากสุด)
--   สิทธิ์แก้ไข reuse permission เดิม production:standard-edit (ไม่สร้าง permission ใหม่)
-- Re-run safety: Idempotent — IF NOT EXISTS / WHERE NOT EXISTS ทั้งหมด
-- =============================================

CREATE TABLE IF NOT EXISTS tbt_production_delivery_target (
    id              BIGSERIAL,
    target_percent  NUMERIC NOT NULL,
    effective_from  TIMESTAMPTZ NOT NULL,
    remark          CHARACTER VARYING,
    create_date     TIMESTAMPTZ NOT NULL,
    create_by       CHARACTER VARYING NOT NULL,
    CONSTRAINT tbt_production_delivery_target_pk PRIMARY KEY (id),
    CONSTRAINT tbt_production_delivery_target_percent_ck CHECK (target_percent >= 0 AND target_percent <= 100)
);

CREATE INDEX IF NOT EXISTS idx_tbt_production_delivery_target_effective
    ON tbt_production_delivery_target (effective_from DESC);

-- Seed ค่าเริ่มต้น 80% — ข้ามถ้ามีแถวอยู่แล้ว (idempotent)
INSERT INTO tbt_production_delivery_target (target_percent, effective_from, remark, create_date, create_by)
SELECT 80, NOW(), 'ค่าเริ่มต้น', NOW(), 'system'
WHERE NOT EXISTS (SELECT 1 FROM tbt_production_delivery_target);

-- =============================================
-- Verification (uncomment to run manually)
-- =============================================
-- SELECT * FROM tbt_production_delivery_target ORDER BY effective_from DESC;
