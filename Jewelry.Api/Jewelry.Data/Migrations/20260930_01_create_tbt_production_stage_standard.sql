-- =============================================
-- Migration: Stage Standard (มาตรฐานเวลาแต่ละแผนกผลิต) + permission production:standard-edit
-- Date: 2026-09-30
-- Description: ตาราง append-only history เก็บ "มาตรฐานวัน" ต่อแผนก ใช้คำนวณ lead time /
--   capacity ของหน้า ProductionInsight — ทุกครั้งที่บันทึกค่าใหม่ = insert แถวใหม่ (ไม่ update ของเดิม)
--   ค่าที่ใช้งานจริง ณ เวลาใดๆ = แถวล่าสุดของแต่ละ dept_key ที่ effective_from <= เวลานั้น
-- Re-run safety: Idempotent — IF NOT EXISTS / ON CONFLICT DO NOTHING / WHERE NOT EXISTS ทั้งหมด
-- =============================================

-- =============================================
-- 1. tbt_production_stage_standard
-- =============================================
CREATE TABLE IF NOT EXISTS tbt_production_stage_standard (
    id              BIGSERIAL,
    dept_key        CHARACTER VARYING NOT NULL,
    -- dept_key: design, trim, rawPolish, gemSort, setting, plating, costCard (ตรงกับ ProductionPlanDepartments)
    standard_days   NUMERIC NOT NULL,
    effective_from  TIMESTAMPTZ NOT NULL,
    remark          CHARACTER VARYING,
    create_date     TIMESTAMPTZ NOT NULL,
    create_by       CHARACTER VARYING NOT NULL,
    CONSTRAINT tbt_production_stage_standard_pk PRIMARY KEY (id)
);

CREATE INDEX IF NOT EXISTS idx_tbt_production_stage_standard_dept_effective
    ON tbt_production_stage_standard (dept_key, effective_from DESC);

-- =============================================
-- 2. Seed ค่าเริ่มต้น 14 วัน ต่อแผนก — ข้ามถ้า dept นั้นมีแถวอยู่แล้ว (idempotent)
-- =============================================
INSERT INTO tbt_production_stage_standard (dept_key, standard_days, effective_from, remark, create_date, create_by)
SELECT dept.key, 14, NOW(), 'ค่าเริ่มต้น', NOW(), 'system'
FROM (VALUES ('design'), ('trim'), ('rawPolish'), ('gemSort'), ('setting'), ('plating'), ('costCard')) AS dept(key)
WHERE NOT EXISTS (
    SELECT 1 FROM tbt_production_stage_standard s WHERE s.dept_key = dept.key
);

-- =============================================
-- 3. Permission production:standard-edit — แก้ไขมาตรฐานเวลา (Executive + Dev เท่านั้น)
-- =============================================
INSERT INTO tbm_permission (code, name, group_name, create_by) VALUES
    ('production:standard-edit', 'แก้ไขมาตรฐานเวลาแต่ละแผนก (Stage Standard)', 'Production', 'system')
ON CONFLICT (code) DO NOTHING;

INSERT INTO tbt_role_permission (role_id, permission_id, create_by)
SELECT r.id, p.id, 'system'
FROM tbm_user_role r
CROSS JOIN tbm_permission p
WHERE r.name IN ('Executive', 'Dev') AND r.is_active = TRUE
    AND p.code = 'production:standard-edit' AND p.is_active = TRUE
ON CONFLICT (role_id, permission_id) DO NOTHING;

-- =============================================
-- Verification (uncomment to run manually)
-- =============================================
-- SELECT dept_key, standard_days, effective_from, create_by, remark
-- FROM tbt_production_stage_standard
-- ORDER BY dept_key, effective_from DESC;
--
-- SELECT r.id, r.name FROM tbm_user_role r
-- JOIN tbt_role_permission rp ON rp.role_id = r.id
-- JOIN tbm_permission p ON p.id = rp.permission_id
-- WHERE p.code = 'production:standard-edit';
