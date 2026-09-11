-- =============================================
-- Migration: Seed notification:team Permission
-- Date: 2026-09-10
-- Description: เพิ่ม permission สำหรับดูงานค้างของทีม (มุมมองหัวหน้า) ใน Notification Center
--   ต้อง run หลัง 20260910_01_create_tbt_notification.sql
--   ⚠️ user ต้อง logout + login ใหม่หลัง run migration นี้ ถึงจะได้ permission ชุดใหม่
--      เพราะ permission ถูก cache ไว้ที่ localStorage (permissions-dk) ตอน login เท่านั้น
-- Re-run safety: Idempotent — ON CONFLICT DO NOTHING, รันซ้ำได้ไม่ error
-- =============================================

-- หมายเหตุ: การเห็น notification ของตัวเองไม่ต้องมี permission นี้ — ทุก role เห็น inbox ของตัวเองเสมอ
-- permission นี้ใช้เฉพาะมุมมองหัวหน้า (ดูงานค้างของทีม/ลูกทีมคนอื่น)

-- 1. Insert permission
INSERT INTO tbm_permission (code, name, group_name, create_by) VALUES
    ('notification:team', 'ดูงานค้างของทีม', 'Notification', 'system')
ON CONFLICT (code) DO NOTHING;

-- 2. Admin, Dev: ได้ notification:team
INSERT INTO tbt_role_permission (role_id, permission_id, create_by)
SELECT r.id, p.id, 'system'
FROM tbm_user_role r
CROSS JOIN tbm_permission p
WHERE r.name IN ('Admin', 'Dev') AND r.is_active = TRUE
  AND p.code = 'notification:team'
  AND p.is_active = TRUE
ON CONFLICT (role_id, permission_id) DO NOTHING;
