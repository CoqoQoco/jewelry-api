-- =============================================
-- Migration: Seed stock-rfid:view / stock-rfid:bind Permissions
-- Date: 2026-10-18
-- Description: permission สำหรับ RFID handheld (StockRfid/*) ซึ่ง API บังคับด้วย [RequirePermission]
--   stock-rfid:view  = GetByEpc, GetByEpcs, GetByStock, ListReceipts, ListByReceipt, ListUnbound, ListRecent
--   stock-rfid:bind  = Bind, Unbind
--   ต้อง run หลัง create_permission_tables.sql และ "ก่อน" deploy API/UI ที่เช็ค permission นี้
--   (ถ้า deploy API ก่อน run SQL ทุก role จะโดน 403 ที่ StockRfid/*)
--
--   Role ที่ได้รับ (grant ทั้ง view + bind):
--     1. ทุก role ที่มี stock-product:view อยู่แล้ว (data-driven ไม่ hardcode ชื่อ role)
--        ตาม seed ตั้งต้น = Dev, Admin, StockOperator (+ role อื่นที่ถูกเพิ่ม stock-product:view ภายหลัง)
--     2. Dev และ Admin แบบระบุชื่อ (กันกรณีสองบทบาทนี้ไม่มี stock-product:view จริงบน prod)
--   Role ที่ "ไม่ได้" : Sale, Operator, ProductionOperator, Executive ฯลฯ ที่ไม่มี stock-product:view
--     (Sale ที่ต้อง GetByEpc ตอนขายด้วย RFID ในเฟส 5 ต้อง grant stock-rfid:view เพิ่มภายหลัง)
--   ⚠️ user ต้อง logout + login ใหม่หลัง run (roles ฝังใน JWT, permission cache ที่ localStorage permissions-dk)
-- Re-run safety: Idempotent — ON CONFLICT DO NOTHING, รันซ้ำได้ไม่ error
-- =============================================

-- 1. Insert permissions
INSERT INTO tbm_permission (code, name, group_name, create_by) VALUES
    ('stock-rfid:view', 'ดู/ค้นหาข้อมูลผูกแท็ก RFID', 'Stock Product', 'system'),
    ('stock-rfid:bind', 'ผูก/ยกเลิกผูกแท็ก RFID กับสินค้า', 'Stock Product', 'system')
ON CONFLICT (code) DO NOTHING;

-- 2. Grant ให้ทุก role ที่มี stock-product:view อยู่แล้ว
INSERT INTO tbt_role_permission (role_id, permission_id, create_by)
SELECT DISTINCT rp.role_id, p.id, 'system'
FROM tbt_role_permission rp
JOIN tbm_permission stock_product_view ON stock_product_view.id = rp.permission_id AND stock_product_view.code = 'stock-product:view'
CROSS JOIN tbm_permission p
WHERE p.code IN ('stock-rfid:view', 'stock-rfid:bind') AND p.is_active = TRUE
ON CONFLICT (role_id, permission_id) DO NOTHING;

-- 3. Grant ให้ Dev และ Admin แบบระบุชื่อ
INSERT INTO tbt_role_permission (role_id, permission_id, create_by)
SELECT r.id, p.id, 'system'
FROM tbm_user_role r
CROSS JOIN tbm_permission p
WHERE r.name IN ('Dev', 'Admin') AND r.is_active = TRUE
    AND p.code IN ('stock-rfid:view', 'stock-rfid:bind') AND p.is_active = TRUE
ON CONFLICT (role_id, permission_id) DO NOTHING;

-- =============================================
-- Verification (uncomment to run manually)
-- =============================================
-- SELECT r.name, p.code
-- FROM tbt_role_permission rp
-- JOIN tbm_user_role r ON r.id = rp.role_id
-- JOIN tbm_permission p ON p.id = rp.permission_id
-- WHERE p.code LIKE 'stock-rfid:%'
-- ORDER BY r.name, p.code;
