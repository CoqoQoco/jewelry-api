-- =============================================
-- Migration: Seed announcement:manage Permission
-- Date: 2026-09-08
-- Description: เพิ่ม permission สำหรับจัดการประกาศข่าว (Announcement)
--   ต้อง run หลัง 20260908_03_create_tbt_announcement.sql
--   ⚠️ user ต้อง logout + login ใหม่หลัง run migration นี้ ถึงจะได้ permission ชุดใหม่
--      เพราะ permission ถูก cache ไว้ที่ localStorage (permissions-dk) ตอน login เท่านั้น
-- =============================================

-- 1. Insert permission
INSERT INTO tbm_permission (code, name, group_name, create_by) VALUES
    ('announcement:manage', 'จัดการประกาศข่าว', 'Announcement', 'system')
ON CONFLICT (code) DO NOTHING;

-- 2. Admin, Dev: ได้ announcement:manage
INSERT INTO tbt_role_permission (role_id, permission_id, create_by)
SELECT r.id, p.id, 'system'
FROM tbm_user_role r
CROSS JOIN tbm_permission p
WHERE r.name IN ('Admin', 'Dev') AND r.is_active = TRUE
  AND p.code = 'announcement:manage'
  AND p.is_active = TRUE
ON CONFLICT (role_id, permission_id) DO NOTHING;
