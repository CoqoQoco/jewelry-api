-- =============================================
-- Migration: Seed production:backdate-close Permission
-- Date: 2026-10-15
-- Description: เพิ่ม permission สำหรับ "ปิดใบงานย้อนหลัง" (Production/Plan/BackdateClose*)
--   ต้อง run ก่อน deploy API — ไม่งั้น endpoint จะ 403 ทุกคน
--   user ต้อง logout + login ใหม่หลัง run migration นี้ (permission cache ที่ localStorage permissions-dk)
-- =============================================

INSERT INTO tbm_permission (code, name, group_name, create_by) VALUES
    ('production:backdate-close', 'ปิดใบงานย้อนหลัง', 'Production', 'system')
ON CONFLICT (code) DO NOTHING;

INSERT INTO tbt_role_permission (role_id, permission_id, create_by)
SELECT r.id, p.id, 'system'
FROM tbm_user_role r
CROSS JOIN tbm_permission p
WHERE r.name IN ('Admin', 'Dev') AND r.is_active = TRUE
  AND p.code = 'production:backdate-close'
  AND p.is_active = TRUE
ON CONFLICT (role_id, permission_id) DO NOTHING;
