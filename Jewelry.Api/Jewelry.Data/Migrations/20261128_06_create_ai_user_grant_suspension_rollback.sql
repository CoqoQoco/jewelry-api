-- Rollback: สิทธิ์ข้อมูล AI รายคน + การระงับ + permission ai:admin — TK202611280006
-- ข้อมูล grant / suspension จะหายถาวร · ต้อง rollback API ก่อน (API อ่านตารางเหล่านี้)
DELETE FROM tbt_role_permission WHERE permission_id IN (SELECT id FROM tbm_permission WHERE code = 'ai:admin');
DELETE FROM tbm_permission WHERE code = 'ai:admin';
DROP TABLE IF EXISTS tbt_ai_user_suspension;
DROP TABLE IF EXISTS tbt_ai_user_grant;
