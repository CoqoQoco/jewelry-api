-- =============================================
-- Migration: Seed ticket:manage Permission
-- Date: 2026-06-22
-- Description: เพิ่ม permission สำหรับจัดการ Ticket (dev-only endpoints)
-- =============================================

-- 1. Insert permission
INSERT INTO tbm_permission (code, name, group_name, create_by) VALUES
    ('ticket:manage', 'จัดการ Ticket', 'Ticket', 'system')
ON CONFLICT (code) DO NOTHING;

-- 2. Dev: ได้ ticket:manage
INSERT INTO tbt_role_permission (role_id, permission_id, create_by)
SELECT r.id, p.id, 'system'
FROM tbm_user_role r
CROSS JOIN tbm_permission p
WHERE r.name = 'Dev' AND r.is_active = TRUE
  AND p.code = 'ticket:manage'
  AND p.is_active = TRUE
ON CONFLICT (role_id, permission_id) DO NOTHING;

-- 3. Admin: ได้ ticket:manage
INSERT INTO tbt_role_permission (role_id, permission_id, create_by)
SELECT r.id, p.id, 'system'
FROM tbm_user_role r
CROSS JOIN tbm_permission p
WHERE r.name = 'Admin' AND r.is_active = TRUE
  AND p.code = 'ticket:manage'
  AND p.is_active = TRUE
ON CONFLICT (role_id, permission_id) DO NOTHING;
