-- =============================================
-- Migration: Seed setting:print-layout Permission
-- Date: 2026-08-08
-- Description: เพิ่ม permission สำหรับหน้าตั้งค่ารูปแบบพิมพ์ VAT / Bill (เดิมเกาะ user:dev)
-- =============================================

-- 1. Insert permission
INSERT INTO tbm_permission (code, name, group_name, create_by) VALUES
    ('setting:print-layout', 'ตั้งค่ารูปแบบพิมพ์', 'Setting', 'system')
ON CONFLICT (code) DO NOTHING;

-- 2. Dev: ได้ setting:print-layout
INSERT INTO tbt_role_permission (role_id, permission_id, create_by)
SELECT r.id, p.id, 'system'
FROM tbm_user_role r
CROSS JOIN tbm_permission p
WHERE r.name = 'Dev' AND r.is_active = TRUE
  AND p.code = 'setting:print-layout'
  AND p.is_active = TRUE
ON CONFLICT (role_id, permission_id) DO NOTHING;

-- role อื่นให้ผู้ดูแลติ๊กเพิ่มเองจากหน้า "จัดการสิทธิ์"
