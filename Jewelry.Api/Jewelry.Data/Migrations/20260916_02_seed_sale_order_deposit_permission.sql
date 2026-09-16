-- =============================================
-- Migration: Seed sale:deposit Permission
-- Date: 2026-09-16
-- Description: เพิ่ม permission สำหรับรับมัดจำ/ลบมัดจำใบสั่งขาย (Sale Order Deposit)
--   ต้อง run หลัง create_permission_tables.sql (ต้องมี tbm_permission/tbt_role_permission อยู่แล้ว)
--   Grant ให้ทุก role ที่มี sale:create อยู่แล้วในฐานข้อมูลจริง (data-driven ไม่ hardcode ชื่อ role)
--   ⚠️ user ต้อง logout + login ใหม่หลัง run migration นี้ ถึงจะได้ permission ชุดใหม่ (cache ที่ localStorage permissions-dk)
-- Re-run safety: Idempotent — ON CONFLICT DO NOTHING, รันซ้ำได้ไม่ error
-- =============================================

-- 1. Insert permission
INSERT INTO tbm_permission (code, name, group_name, create_by) VALUES
    ('sale:deposit', 'รับ/ลบมัดจำใบสั่งขาย', 'Sale', 'system')
ON CONFLICT (code) DO NOTHING;

-- 2. Grant ให้ทุก role ที่มี sale:create อยู่แล้ว
INSERT INTO tbt_role_permission (role_id, permission_id, create_by)
SELECT DISTINCT rp.role_id, p.id, 'system'
FROM tbt_role_permission rp
JOIN tbm_permission sale_create ON sale_create.id = rp.permission_id AND sale_create.code = 'sale:create'
CROSS JOIN tbm_permission p
WHERE p.code = 'sale:deposit' AND p.is_active = TRUE
ON CONFLICT (role_id, permission_id) DO NOTHING;
