-- =============================================
-- Migration: Seed material:manage-min Permission
-- Date: 2026-10-22
-- Description: permission สำหรับแก้ไขนโยบาย/ขั้นต่ำของแท็บ "คลังวัตถุดิบ" (/executive)
--   (Policy/Save, Min/Save, Min/Delete) — การดูหน้า (Overview/Policy/Get) ใช้ executive:view เดิม
--   grant ให้ role Dev และ StockOperator เท่านั้น (lookup id จากชื่อ ไม่ hardcode id)
--   ต้อง run migration นี้ "ก่อน" deploy API/UI ที่เช็ค permission material:manage-min
--   ⚠️ user ต้อง logout + login ใหม่หลัง run migration นี้ ถึงจะได้ permission ชุดใหม่
--      (permission cache ที่ localStorage permissions-dk, roles ฝังอยู่ใน JWT ตอน login)
-- Re-run safety: Idempotent — ON CONFLICT DO NOTHING, รันซ้ำได้ไม่ error
-- =============================================

-- 1. Insert permission (group_name ใช้ 'Stock Gem' เดียวกับ stock-gold:* )
INSERT INTO tbm_permission (code, name, group_name, create_by) VALUES
    ('material:manage-min', 'จัดการขั้นต่ำ/นโยบายคลังวัตถุดิบ', 'Stock Gem', 'system')
ON CONFLICT (code) DO NOTHING;

-- 2. Grant ให้ Dev และ StockOperator
INSERT INTO tbt_role_permission (role_id, permission_id, create_by)
SELECT r.id, p.id, 'system'
FROM tbm_user_role r
CROSS JOIN tbm_permission p
WHERE r.name IN ('Dev', 'StockOperator') AND r.is_active = TRUE
    AND p.code = 'material:manage-min' AND p.is_active = TRUE
ON CONFLICT (role_id, permission_id) DO NOTHING;

-- =============================================
-- Verification (uncomment to run manually)
-- =============================================
-- SELECT r.id, r.name, r.level, r.is_active
-- FROM tbm_user_role r
-- JOIN tbt_role_permission rp ON rp.role_id = r.id
-- JOIN tbm_permission p ON p.id = rp.permission_id
-- WHERE p.code = 'material:manage-min'
-- ORDER BY r.id;
