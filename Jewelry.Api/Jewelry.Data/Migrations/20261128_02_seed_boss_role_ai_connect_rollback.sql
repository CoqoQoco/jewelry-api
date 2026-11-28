-- =============================================
-- Rollback: Seed role Boss + ย้ายสิทธิ์ ai:connect
-- Date: 2026-11-28
-- Ticket: TK202611280002
-- Description: ย้อนกลับ 20261128_02_seed_boss_role_ai_connect.sql
--   - คืน ai:connect ให้ role Executive
--   - ลบ grant ai:connect + executive:view ของ role Dev (ตามที่ migration เพิ่ม)
--   - ลบ grant ทั้งหมดของ role Boss
--   - ลบ role Boss "เฉพาะเมื่อไม่มี user ใช้ role นี้" (tbt_user_role.role = tbm_user_role.id)
--   - คืนชื่อ permission ai:connect เป็นชื่อเดิม
--   NOTICE: ถ้ายังมี user ผูก role Boss อยู่ role จะไม่ถูกลบ (เหลือไว้โดยไม่มี permission)
--           ต้องถอด user ออกจาก role ก่อน แล้วรันบล็อก 4 ซ้ำ
--   ⚠️ ถ้า Dev เคยมี executive:view ก่อนรัน migration นี้ การลบในบล็อก 2 จะลบของเดิมด้วย — ตรวจก่อนรัน
-- =============================================

BEGIN;

-- 1. คืน ai:connect ให้ Executive
INSERT INTO tbt_role_permission (role_id, permission_id, create_by)
SELECT r.id, p.id, 'system'
FROM tbm_user_role r
CROSS JOIN tbm_permission p
WHERE r.name = 'Executive' AND r.is_active = TRUE
    AND p.code = 'ai:connect' AND p.is_active = TRUE
ON CONFLICT (role_id, permission_id) DO NOTHING;

-- 2. ลบ grant ของ Dev
DELETE FROM tbt_role_permission
WHERE role_id IN (SELECT id FROM tbm_user_role WHERE name = 'Dev')
    AND permission_id IN (SELECT id FROM tbm_permission WHERE code IN ('ai:connect', 'executive:view'));

-- 3. ลบ grant ของ Boss
DELETE FROM tbt_role_permission
WHERE role_id IN (SELECT id FROM tbm_user_role WHERE name = 'Boss');

-- 4. ลบ role Boss เฉพาะเมื่อไม่มี user ใช้
DELETE FROM tbm_user_role r
WHERE r.name = 'Boss'
    AND NOT EXISTS (SELECT 1 FROM tbt_user_role ur WHERE ur.role = r.id)
    AND NOT EXISTS (SELECT 1 FROM tbt_role_permission rp WHERE rp.role_id = r.id);

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM tbm_user_role WHERE name = 'Boss') THEN
        RAISE NOTICE 'role Boss ยังมี user ผูกอยู่ จึงไม่ถูกลบ — ถอด user ออกจาก role ก่อนแล้วรันบล็อก 4 ซ้ำ';
    END IF;
END $$;

-- 5. คืนชื่อ permission เดิม
UPDATE tbm_permission
SET name = 'เชื่อมต่อ AI (MCP) เข้ากับรายงานผู้บริหาร', update_date = NOW(), update_by = 'system'
WHERE code = 'ai:connect';

COMMIT;

-- =============================================
-- Verification (uncomment to run manually)
-- =============================================
-- SELECT * FROM tbm_user_role WHERE name = 'Boss';
-- SELECT r.name, p.code FROM tbm_user_role r
-- JOIN tbt_role_permission rp ON rp.role_id = r.id
-- JOIN tbm_permission p ON p.id = rp.permission_id
-- WHERE p.code = 'ai:connect';
