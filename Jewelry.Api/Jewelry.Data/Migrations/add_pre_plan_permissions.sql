-- =============================================
-- Migration: Add Pre-Plan Permissions
-- Date: 2026-05-04
-- Description: เพิ่ม permission สำหรับ Pre-Plan (ใบสั่งผลิต)
-- =============================================

-- 1. Insert permissions ใหม่
INSERT INTO tbm_permission (code, name, group_name, create_by) VALUES
    ('pre-plan:view',    'ดูใบสั่งผลิต',     'Pre-Plan', 'system'),
    ('pre-plan:create',  'สร้างใบสั่งผลิต',   'Pre-Plan', 'system'),
    ('pre-plan:approve', 'อนุมัติใบสั่งผลิต', 'Pre-Plan', 'system')
ON CONFLICT (code) DO NOTHING;

-- 2. Dev: ได้ทุก permission (รวม pre-plan ด้วย)
INSERT INTO tbt_role_permission (role_id, permission_id, create_by)
SELECT r.id, p.id, 'system'
FROM tbm_user_role r
CROSS JOIN tbm_permission p
WHERE r.name = 'Dev' AND r.is_active = TRUE
  AND p.code IN ('pre-plan:view', 'pre-plan:create', 'pre-plan:approve')
  AND p.is_active = TRUE
ON CONFLICT (role_id, permission_id) DO NOTHING;

-- 3. Admin: ได้ทุก pre-plan permission
INSERT INTO tbt_role_permission (role_id, permission_id, create_by)
SELECT r.id, p.id, 'system'
FROM tbm_user_role r
CROSS JOIN tbm_permission p
WHERE r.name = 'Admin' AND r.is_active = TRUE
  AND p.code IN ('pre-plan:view', 'pre-plan:create', 'pre-plan:approve')
  AND p.is_active = TRUE
ON CONFLICT (role_id, permission_id) DO NOTHING;

-- 4. Operator: view + create เท่านั้น
INSERT INTO tbt_role_permission (role_id, permission_id, create_by)
SELECT r.id, p.id, 'system'
FROM tbm_user_role r
CROSS JOIN tbm_permission p
WHERE r.name = 'Operator' AND r.is_active = TRUE
  AND p.code IN ('pre-plan:view', 'pre-plan:create')
  AND p.is_active = TRUE
ON CONFLICT (role_id, permission_id) DO NOTHING;

-- 5. ProductionOperator: view + create เท่านั้น
INSERT INTO tbt_role_permission (role_id, permission_id, create_by)
SELECT r.id, p.id, 'system'
FROM tbm_user_role r
CROSS JOIN tbm_permission p
WHERE r.name = 'ProductionOperator' AND r.is_active = TRUE
  AND p.code IN ('pre-plan:view', 'pre-plan:create')
  AND p.is_active = TRUE
ON CONFLICT (role_id, permission_id) DO NOTHING;
