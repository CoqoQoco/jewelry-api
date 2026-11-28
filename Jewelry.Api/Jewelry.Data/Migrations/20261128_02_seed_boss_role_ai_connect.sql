-- =============================================
-- Migration: Seed role Boss + ย้ายสิทธิ์ ai:connect จาก Executive ไป Boss/Dev
-- Date: 2026-11-28
-- Ticket: TK202611280002
-- Description: เพิ่ม role Boss (บอส — เชื่อม AI กับระบบ + ดูหน้าผู้บริหาร)
--   - Boss ได้: executive:view, report:view, sale:view-margin, production:standard-edit, ai:connect
--   - Dev ได้เพิ่ม: ai:connect, executive:view
--   - ถอด ai:connect ออกจาก role Executive (Executive เสียแค่ ai:connect — ยังมี executive:view, report:view, sale:view-margin, production:standard-edit)
--   - เปลี่ยนชื่อ permission ai:connect เป็น 'เชื่อมต่อ AI (MCP) กับระบบ'
--   ต้องรัน 20261124_01_ai_connector.sql ก่อน (prod รันแล้ว — มี ai:connect อยู่)
--   ลำดับการรัน: ไม่ผูกกับ deploy (ฝั่ง UI เปลี่ยนเฉพาะข้อความ) รันก่อนหรือหลัง deploy ก็ได้
--   หลังรัน: admin ผูกบัญชีบอสเข้า role Boss ที่หน้าแก้ไขบัญชี แล้วบอสต้อง logout + login ใหม่
--      (permission cache ที่ localStorage permissions-dk, roles ฝังอยู่ใน JWT ตอน login)
--   ⚠️ สมาชิก role Executive จะเสีย ai:connect ทันที — prod ปัจจุบัน (2026-11-28) มีเฉพาะ CoqoAdmin
--      ที่อยู่ใน Executive และอยู่ใน Dev ด้วย และไม่มี AI token ที่ active
-- Re-run safety: Idempotent — WHERE NOT EXISTS + ON CONFLICT DO NOTHING, DELETE/UPDATE รันซ้ำได้ไม่ error
-- =============================================

BEGIN;

-- 1. Create role Boss
--    tbm_user_role: id ไม่มี default (ต้องระบุเอง), create_date ไม่มี default, is_active default FALSE (ต้องระบุ TRUE เอง)
--    prod ปัจจุบัน (2026-11-28) มี id 1-9 และ 999 อยู่แล้ว จึงใช้ id = 10
INSERT INTO tbm_user_role (id, name, description, level, is_active, create_date, create_by)
SELECT 10, 'Boss', 'บอส — เชื่อม AI กับระบบ + ดูหน้าผู้บริหาร', 100, TRUE, NOW(), 'system'
WHERE NOT EXISTS (SELECT 1 FROM tbm_user_role WHERE name = 'Boss')
ON CONFLICT (id) DO NOTHING;

-- 2. Grant ให้ role Boss (lookup id จากชื่อ ไม่ hardcode)
INSERT INTO tbt_role_permission (role_id, permission_id, create_by)
SELECT r.id, p.id, 'system'
FROM tbm_user_role r
CROSS JOIN tbm_permission p
WHERE r.name = 'Boss' AND r.is_active = TRUE
    AND p.code IN ('executive:view', 'report:view', 'sale:view-margin', 'production:standard-edit', 'ai:connect')
    AND p.is_active = TRUE
ON CONFLICT (role_id, permission_id) DO NOTHING;

-- 3. Grant ai:connect + executive:view ให้ role Dev
INSERT INTO tbt_role_permission (role_id, permission_id, create_by)
SELECT r.id, p.id, 'system'
FROM tbm_user_role r
CROSS JOIN tbm_permission p
WHERE r.name = 'Dev' AND r.is_active = TRUE
    AND p.code IN ('ai:connect', 'executive:view') AND p.is_active = TRUE
ON CONFLICT (role_id, permission_id) DO NOTHING;

-- 4. ถอด ai:connect ออกจาก role Executive
DELETE FROM tbt_role_permission
WHERE role_id IN (SELECT id FROM tbm_user_role WHERE name = 'Executive')
    AND permission_id IN (SELECT id FROM tbm_permission WHERE code = 'ai:connect');

-- 5. เปลี่ยนชื่อ permission ai:connect
UPDATE tbm_permission
SET name = 'เชื่อมต่อ AI (MCP) กับระบบ', update_date = NOW(), update_by = 'system'
WHERE code = 'ai:connect';

COMMIT;

-- =============================================
-- Verification (uncomment to run manually)
-- =============================================
-- SELECT r.id, r.name, r.level, r.is_active, p.code
-- FROM tbm_user_role r
-- JOIN tbt_role_permission rp ON rp.role_id = r.id
-- JOIN tbm_permission p ON p.id = rp.permission_id
-- WHERE r.name IN ('Boss', 'Dev', 'Executive')
--     AND p.code IN ('executive:view', 'report:view', 'sale:view-margin', 'production:standard-edit', 'ai:connect')
-- ORDER BY r.name, p.code;
--
-- SELECT id, name, level, is_active FROM tbm_user_role WHERE name = 'Boss';
-- SELECT code, name FROM tbm_permission WHERE code = 'ai:connect';
