-- =============================================
-- Migration: Release log (TK202611060005)
-- Date: 2026-11-06
-- Description: บันทึกรอบการ release/ประกาศอัปเดตระบบ 1 แถวต่อ 1 รอบ
--   announcement_id = tbt_announcement.id (อ้างอิงอย่างเดียว ไม่มี FK)
--   ticket_nos / closed_ticket_nos = jsonb array ของเลข TKxxxxxxxxxxxx
--   manual_steps = jsonb array ของ { "desc": text, "done": boolean }
-- Re-run safety: Idempotent — CREATE TABLE IF NOT EXISTS
-- ลำดับ: รัน SQL ก่อน deploy API
-- =============================================

CREATE TABLE IF NOT EXISTS tbt_release_log (
    id                  BIGSERIAL NOT NULL,
    announcement_id     BIGINT,
    ui_commit           CHARACTER VARYING,
    api_commit          CHARACTER VARYING,
    app_version         CHARACTER VARYING,
    ticket_nos          JSONB NOT NULL DEFAULT '[]'::jsonb,
    closed_ticket_nos   JSONB NOT NULL DEFAULT '[]'::jsonb,
    manual_steps        JSONB NOT NULL DEFAULT '[]'::jsonb,
    note                CHARACTER VARYING,
    create_date         TIMESTAMPTZ NOT NULL,
    create_by           CHARACTER VARYING NOT NULL,
    CONSTRAINT tbt_release_log_pk PRIMARY KEY (id)
);
