-- =============================================
-- Migration: Seed executive:view Permission + Executive Role
-- Date: 2026-09-28
-- Description: เพิ่ม permission สำหรับหน้า "ภาพรวมผู้บริหาร" (/executive) ซึ่งแสดงตัวเลขเงิน
--   (ลูกหนี้ค้างรับ, มูลค่าคลัง, ทองจมในสินค้าเก่า) — เห็นเฉพาะผู้บริหารเท่านั้น
--   ต้อง run migration นี้ "ก่อน" deploy API/UI ที่เช็ค permission executive:view
--   ⚠️ user ต้อง logout + login ใหม่หลังถูกผูก role ถึงจะได้ permission ชุดใหม่
--      (permission cache ที่ localStorage permissions-dk, roles ฝังอยู่ใน JWT ตอน login)
--   ⚠️ grant ให้ role Executive "เท่านั้น" — ไม่ให้ Dev/Admin โดยตั้งใจ
--      ถ้าต้องการทดสอบ ให้ผูก user ทดสอบเข้า role Executive ชั่วคราวแล้วถอดออก
--   Executive role มีแค่ permission เดียว — user ที่ได้ role นี้ยังคง role เดิมของตัวเองได้ตามปกติ
-- Re-run safety: Idempotent — ON CONFLICT DO NOTHING, รันซ้ำได้ไม่ error
-- =============================================

-- 1. Insert permission
INSERT INTO tbm_permission (code, name, group_name, create_by) VALUES
    ('executive:view', 'ดูหน้าภาพรวมผู้บริหาร (ตัวเลขเงิน/ลูกหนี้/มูลค่าคลัง)', 'Report', 'system')
ON CONFLICT (code) DO NOTHING;

-- 2. Create role Executive
--    tbm_user_role: id ไม่มี default (ต้องระบุเอง), create_date ไม่มี default (ต้องระบุเอง), is_active default FALSE (ต้องระบุ TRUE เอง)
--    prod ปัจจุบัน (2026-09-28) มี id 1-7 และ 999 อยู่แล้ว จึงใช้ id = 8
--    level = 100 เท่ากับ role ทั่วไป — role นี้ให้สิทธิ์ดูหน้าเดียว ไม่ยกระดับสิทธิ์อื่น
INSERT INTO tbm_user_role (id, name, description, level, is_active, create_date, create_by)
SELECT 8, 'Executive', 'ผู้บริหาร — ดูหน้าภาพรวมผู้บริหาร (/executive)', 100, TRUE, NOW(), 'system'
WHERE NOT EXISTS (SELECT 1 FROM tbm_user_role WHERE name = 'Executive')
ON CONFLICT (id) DO NOTHING;

-- 3. Grant executive:view ให้ role Executive เท่านั้น (lookup id จากชื่อ ไม่ hardcode id)
INSERT INTO tbt_role_permission (role_id, permission_id, create_by)
SELECT r.id, p.id, 'system'
FROM tbm_user_role r
CROSS JOIN tbm_permission p
WHERE r.name = 'Executive' AND r.is_active = TRUE
    AND p.code = 'executive:view' AND p.is_active = TRUE
ON CONFLICT (role_id, permission_id) DO NOTHING;

-- =============================================
-- Verification (uncomment to run manually)
-- =============================================
-- SELECT r.id, r.name, r.level, r.is_active
-- FROM tbm_user_role r
-- JOIN tbt_role_permission rp ON rp.role_id = r.id
-- JOIN tbm_permission p ON p.id = rp.permission_id
-- WHERE p.code = 'executive:view'
-- ORDER BY r.id;
