-- Rollback: AI Connector (MCP) — TK202611240007
-- ลบ role permission ก่อน แล้วลบ permission และตาราง (ข้อมูล token/log จะหายถาวร)
-- ลบเฉพาะแถว grant sale:view-margin ของ role Executive (ห้ามลบตัว permission — Dev/SaleManager ใช้อยู่)
-- ถ้า Executive เคยได้ sale:view-margin มาก่อนรัน migration นี้ แถวนั้นจะถูกลบด้วย
DELETE FROM tbt_role_permission
WHERE role_id IN (SELECT id FROM tbm_user_role WHERE name = 'Executive')
  AND permission_id IN (SELECT id FROM tbm_permission WHERE code = 'sale:view-margin');
DELETE FROM tbt_role_permission WHERE permission_id IN (SELECT id FROM tbm_permission WHERE code = 'ai:connect');
DELETE FROM tbm_permission WHERE code = 'ai:connect';

DROP TABLE IF EXISTS tbt_ai_access_log;
DROP TABLE IF EXISTS tbt_ai_token;
DROP TABLE IF EXISTS tbt_ai_auth_code;
DROP TABLE IF EXISTS tbt_ai_client;
DROP TABLE IF EXISTS tbm_ai_glossary;
DROP TABLE IF EXISTS tbm_ai_scope;
