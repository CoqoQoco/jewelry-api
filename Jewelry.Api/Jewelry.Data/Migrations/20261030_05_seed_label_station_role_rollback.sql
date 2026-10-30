-- =============================================
-- Rollback: Seed label-station:view Permission + LabelStation Role
-- Date: 2026-10-30
-- Ticket: TK202610300006
-- Description: ย้อนกลับ 20261030_05_seed_label_station_role.sql
--   - ลบ grant ของ label-station:view ทุก role
--   - ลบ permission label-station:view
--   - ลบ role LabelStation "เฉพาะเมื่อไม่มี user ใช้ role นี้" (tbt_user_role.role = tbm_user_role.id)
--   NOTICE: ถ้ายังมี user ผูก role LabelStation อยู่ role จะไม่ถูกลบ (เหลือไว้โดยไม่มี permission)
--           ต้องถอด user ออกจาก role ก่อน แล้วรันบล็อก 3 ซ้ำ
-- =============================================

-- 1. ลบ grant
DELETE FROM tbt_role_permission
WHERE permission_id IN (SELECT id FROM tbm_permission WHERE code = 'label-station:view');

-- 2. ลบ permission
DELETE FROM tbm_permission WHERE code = 'label-station:view';

-- 3. ลบ role LabelStation เฉพาะเมื่อไม่มี user ใช้
DELETE FROM tbm_user_role r
WHERE r.name = 'LabelStation'
    AND NOT EXISTS (SELECT 1 FROM tbt_user_role ur WHERE ur.role = r.id)
    AND NOT EXISTS (SELECT 1 FROM tbt_role_permission rp WHERE rp.role_id = r.id);

-- =============================================
-- Verification (uncomment to run manually)
-- =============================================
-- SELECT * FROM tbm_permission WHERE code = 'label-station:view';
-- SELECT * FROM tbm_user_role WHERE name = 'LabelStation';
