-- Rollback: 20261106_02_create_tbt_release_log.sql
-- ลบตาราง release log ทั้งหมด (ข้อมูลหาย) — รันหลัง rollback API แล้วเท่านั้น
DROP TABLE IF EXISTS tbt_release_log;
