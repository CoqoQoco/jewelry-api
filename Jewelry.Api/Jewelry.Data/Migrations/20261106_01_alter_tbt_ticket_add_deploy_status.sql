-- =============================================
-- Migration: Ticket deploy status + commit (TK202611060005)
-- Date: 2026-11-06
-- Description: เพิ่มคอลัมน์ติดตามการ deploy ของ ticket
--   deploy_status = none (ยังไม่ deploy) | pending (รอ deploy) | deployed (deploy แล้ว)
--   deploy_date   = เวลาที่ตั้งเป็น deployed (UTC) — NULL ถ้ายังไม่ deployed
--   commit_ui / commit_api = git SHA (คั่น comma ได้) — NULL ถ้ายังไม่ระบุ
-- Re-run safety: Idempotent — ADD COLUMN IF NOT EXISTS / constraint ตรวจก่อนสร้าง
-- ลำดับ: รัน SQL ก่อน deploy API
-- =============================================

ALTER TABLE tbt_ticket
    ADD COLUMN IF NOT EXISTS deploy_status CHARACTER VARYING NOT NULL DEFAULT 'none',
    ADD COLUMN IF NOT EXISTS deploy_date   TIMESTAMPTZ,
    ADD COLUMN IF NOT EXISTS commit_ui     CHARACTER VARYING,
    ADD COLUMN IF NOT EXISTS commit_api    CHARACTER VARYING;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'tbt_ticket_deploy_status_ck'
    ) THEN
        ALTER TABLE tbt_ticket
            ADD CONSTRAINT tbt_ticket_deploy_status_ck
            CHECK (deploy_status IN ('none', 'pending', 'deployed'));
    END IF;
END $$;
