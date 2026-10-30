-- =============================================
-- Migration: Seed label-station:view Permission + LabelStation Role
-- Date: 2026-10-30
-- Ticket: TK202610300006
-- Description: เพิ่ม permission สำหรับหน้า "เครื่องพิมพ์ป้ายวัตถุดิบ (คิวพิมพ์ป้ายกลาง)" (/label-station)
--   และ role LabelStation สำหรับ user ประจำเครื่องกลางที่พิมพ์ป้าย — เปิดได้เฉพาะหน้า /label-station
--   ต้อง run migration นี้ "ก่อน" deploy UI/API ที่เช็ค permission label-station:view
--   ⚠️ user ต้อง logout + login ใหม่หลังถูกผูก role ถึงจะได้ permission ชุดใหม่
--      (permission cache ที่ localStorage permissions-dk, roles ฝังอยู่ใน JWT ตอน login)
--   Grant label-station:view ให้ role LabelStation และ Dev "เท่านั้น" (lookup จากชื่อ)
-- Re-run safety: Idempotent — WHERE NOT EXISTS + ON CONFLICT DO NOTHING, รันซ้ำได้ไม่ error
-- =============================================

-- 1. Insert permission
INSERT INTO tbm_permission (code, name, group_name, create_by) VALUES
    ('label-station:view', 'เปิดหน้าเครื่องพิมพ์ป้ายวัตถุดิบ (คิวพิมพ์ป้ายกลาง)', 'Stock Gem', 'system')
ON CONFLICT (code) DO NOTHING;

-- 2. Create role LabelStation
--    tbm_user_role: id ไม่มี default (ต้องระบุเอง), create_date ไม่มี default (ต้องระบุเอง), is_active default FALSE (ต้องระบุ TRUE เอง)
--    prod ปัจจุบัน (2026-10-30) มี id 1-8 และ 999 อยู่แล้ว จึงใช้ id = 9
--    level = 100 เท่ากับ role ทั่วไป — role นี้ให้สิทธิ์เปิดหน้าเดียว ไม่ยกระดับสิทธิ์อื่น
INSERT INTO tbm_user_role (id, name, description, level, is_active, create_date, create_by)
SELECT 9, 'LabelStation', 'เครื่องกลางพิมพ์ป้ายวัตถุดิบ — เปิดได้เฉพาะหน้า /label-station', 100, TRUE, NOW(), 'system'
WHERE NOT EXISTS (SELECT 1 FROM tbm_user_role WHERE name = 'LabelStation')
ON CONFLICT (id) DO NOTHING;

-- 3. Grant label-station:view ให้ LabelStation และ Dev เท่านั้น (lookup id จากชื่อ ไม่ hardcode id)
INSERT INTO tbt_role_permission (role_id, permission_id, create_by)
SELECT r.id, p.id, 'system'
FROM tbm_user_role r
CROSS JOIN tbm_permission p
WHERE r.name IN ('LabelStation', 'Dev') AND r.is_active = TRUE
    AND p.code = 'label-station:view' AND p.is_active = TRUE
ON CONFLICT (role_id, permission_id) DO NOTHING;

-- =============================================
-- Verification (uncomment to run manually)
-- =============================================
-- SELECT r.id, r.name, r.level, r.is_active, p.code
-- FROM tbm_user_role r
-- JOIN tbt_role_permission rp ON rp.role_id = r.id
-- JOIN tbm_permission p ON p.id = rp.permission_id
-- WHERE p.code = 'label-station:view'
-- ORDER BY r.id;
--
-- SELECT id, name, level, is_active FROM tbm_user_role WHERE name = 'LabelStation';
