-- =============================================
-- Migration: Add audience to tbt_announcement
-- Date: 2026-09-09
-- Description: เพิ่มคอลัมน์ audience ให้ประกาศเลือกกลุ่มผู้เห็นได้ (ทุกคน / เฉพาะ Dev สำหรับดูก่อนเผยแพร่จริง)
-- Run order: รันหลัง 20260908_03_create_tbt_announcement.sql
-- Re-run safety: Idempotent — ADD COLUMN IF NOT EXISTS + เช็ค pg_constraint ก่อนเพิ่ม CHECK, รันซ้ำได้ไม่ error
-- Data: แถวเดิมทั้งหมดจะได้ค่า default 'all'
-- =============================================

ALTER TABLE tbt_announcement ADD COLUMN IF NOT EXISTS audience VARCHAR(20) NOT NULL DEFAULT 'all';

COMMENT ON COLUMN tbt_announcement.audience IS 'ผู้เห็นประกาศ: all = ทุกคนที่ login, dev = เฉพาะ role Dev (ดูก่อนเผยแพร่)';

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'tbt_announcement_audience_ck'
    ) THEN
        ALTER TABLE tbt_announcement
            ADD CONSTRAINT tbt_announcement_audience_ck CHECK (audience IN ('all', 'dev'));
    END IF;
END $$;
