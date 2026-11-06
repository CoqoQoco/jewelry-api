-- Rollback: 20261106_01_alter_tbt_ticket_add_deploy_status.sql
-- ลบคอลัมน์ deploy/commit ของ ticket (ข้อมูลในคอลัมน์เหล่านี้หาย) — รันหลัง rollback API แล้วเท่านั้น
ALTER TABLE tbt_ticket
    DROP CONSTRAINT IF EXISTS tbt_ticket_deploy_status_ck;

ALTER TABLE tbt_ticket
    DROP COLUMN IF EXISTS deploy_status,
    DROP COLUMN IF EXISTS deploy_date,
    DROP COLUMN IF EXISTS commit_ui,
    DROP COLUMN IF EXISTS commit_api;
