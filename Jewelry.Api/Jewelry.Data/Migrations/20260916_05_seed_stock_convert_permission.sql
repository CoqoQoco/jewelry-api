-- =============================================
-- Migration: Seed stock:convert Permission
-- Date: 2026-09-16
-- Description: เพิ่ม permission สำหรับใบแปลงสินค้า (Stock Convert)
--   ต้อง run หลัง create_permission_tables.sql (ต้องมี tbm_permission/tbt_role_permission อยู่แล้ว)
--   Grant ให้ทุก role ที่มี stock-product:view อยู่แล้ว (permission คลังสินค้าที่ใกล้เคียงที่สุด — data-driven ไม่ hardcode ชื่อ role)
--   ⚠️ user ต้อง logout + login ใหม่หลัง run migration นี้ ถึงจะได้ permission ชุดใหม่ (cache ที่ localStorage permissions-dk)
-- Re-run safety: Idempotent — ON CONFLICT DO NOTHING, รันซ้ำได้ไม่ error
-- =============================================

-- 1. Insert permission
INSERT INTO tbm_permission (code, name, group_name, create_by) VALUES
    ('stock:convert', 'จัดการใบแปลงสินค้า', 'Stock Product', 'system')
ON CONFLICT (code) DO NOTHING;

-- 2. Grant ให้ทุก role ที่มี stock-product:view อยู่แล้ว
INSERT INTO tbt_role_permission (role_id, permission_id, create_by)
SELECT DISTINCT rp.role_id, p.id, 'system'
FROM tbt_role_permission rp
JOIN tbm_permission stock_product_view ON stock_product_view.id = rp.permission_id AND stock_product_view.code = 'stock-product:view'
CROSS JOIN tbm_permission p
WHERE p.code = 'stock:convert' AND p.is_active = TRUE
ON CONFLICT (role_id, permission_id) DO NOTHING;
