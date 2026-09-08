-- =============================================
-- Migration: Create tbt_announcement
-- Date: 2026-09-08
-- Description: ประกาศข่าว (Announcement) — ฟีดข่าวสารแสดงในระบบ, รองรับปักหมุด/กำหนดช่วงเวลาแสดงผล/soft delete
-- Run order: ไม่มี dependency กับตารางอื่น รันได้ทันที
-- Re-run safety: Idempotent — ใช้ CREATE TABLE/INDEX IF NOT EXISTS, รันซ้ำได้ไม่ error
-- =============================================

-- =============================================
-- 1. tbt_announcement
-- =============================================
CREATE TABLE IF NOT EXISTS tbt_announcement (
    id             BIGSERIAL     NOT NULL,
    title          VARCHAR(200)  NOT NULL,
    body           TEXT          NOT NULL,
    image_path     VARCHAR       NULL,
    is_pinned      BOOLEAN       NOT NULL DEFAULT FALSE,
    publish_start  TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    publish_end    TIMESTAMPTZ   NULL,
    is_published   BOOLEAN       NOT NULL DEFAULT TRUE,
    is_active      BOOLEAN       NOT NULL DEFAULT TRUE,
    create_date    TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    create_by      VARCHAR       NOT NULL,
    update_date    TIMESTAMPTZ   NULL,
    update_by      VARCHAR       NULL,
    CONSTRAINT tbt_announcement_pk PRIMARY KEY (id)
);

CREATE INDEX IF NOT EXISTS idx_tbt_announcement_feed ON tbt_announcement (is_active, is_published, publish_start DESC);

COMMENT ON COLUMN tbt_announcement.is_pinned IS 'ปักหมุด — แสดงบนสุดของฟีดเสมอ';
COMMENT ON COLUMN tbt_announcement.publish_start IS 'วันเวลาเริ่มแสดงผล (UTC)';
COMMENT ON COLUMN tbt_announcement.publish_end IS 'วันเวลาสิ้นสุดแสดงผล (UTC) — NULL = ไม่มีวันหมดอายุ';
COMMENT ON COLUMN tbt_announcement.is_published IS 'สถานะเผยแพร่ — FALSE = ซ่อนจากฟีด (draft/ยกเลิกเผยแพร่)';
COMMENT ON COLUMN tbt_announcement.is_active IS 'Soft delete — FALSE = ถูกลบแล้ว';
