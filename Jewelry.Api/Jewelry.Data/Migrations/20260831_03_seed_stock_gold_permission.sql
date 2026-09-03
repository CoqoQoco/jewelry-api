-- =============================================
-- Migration: Seed stock-gold Permission
-- Date: 2026-08-31
-- Description: เพิ่ม permission สำหรับคลังทอง (stock-gold:view, stock-gold:create)
--   ต้อง run หลัง 20260831_01_create_tbt_stock_gold.sql
--   และ 20260831_02_create_tbt_stock_gold_transection.sql
--   ⚠️ user ต้อง logout + login ใหม่หลัง run migration นี้ ถึงจะได้ permission ชุดใหม่
--      เพราะ permission ถูก cache ไว้ที่ localStorage (permissions-dk) ตอน login เท่านั้น
-- =============================================

-- 1. Insert permission (group_name ใช้ 'Stock Gem' เดียวกับ stock-gem:* ตาม create_permission_tables.sql)
INSERT INTO tbm_permission (code, name, group_name, create_by) VALUES
    ('stock-gold:view', 'ดูคลังทอง', 'Stock Gem', 'system'),
    ('stock-gold:create', 'จัดการคลังทอง', 'Stock Gem', 'system')
ON CONFLICT (code) DO NOTHING;

-- 2. stock-gold:view: grant ให้ทุก role ที่มี stock-gem:view อยู่แล้ว
INSERT INTO tbt_role_permission (role_id, permission_id, create_by)
SELECT rp.role_id, p_new.id, 'system'
FROM tbt_role_permission rp
JOIN tbm_permission p_old ON p_old.id = rp.permission_id AND p_old.code = 'stock-gem:view'
JOIN tbm_user_role r ON r.id = rp.role_id AND r.is_active = TRUE
CROSS JOIN tbm_permission p_new
WHERE p_new.code = 'stock-gold:view' AND p_new.is_active = TRUE
ON CONFLICT (role_id, permission_id) DO NOTHING;

-- 3. stock-gold:create: grant ให้ทุก role ที่มี stock-gem:edit อยู่แล้ว
INSERT INTO tbt_role_permission (role_id, permission_id, create_by)
SELECT rp.role_id, p_new.id, 'system'
FROM tbt_role_permission rp
JOIN tbm_permission p_old ON p_old.id = rp.permission_id AND p_old.code = 'stock-gem:edit'
JOIN tbm_user_role r ON r.id = rp.role_id AND r.is_active = TRUE
CROSS JOIN tbm_permission p_new
WHERE p_new.code = 'stock-gold:create' AND p_new.is_active = TRUE
ON CONFLICT (role_id, permission_id) DO NOTHING;
