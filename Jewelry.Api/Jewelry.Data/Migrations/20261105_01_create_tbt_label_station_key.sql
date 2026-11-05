-- =============================================
-- Migration: Create tbt_label_station_key
-- Date: 2026-11-05
-- Ticket: TK202611050005
-- Description: device key ของเครื่องกลาง /label-station (header X-Device-Key) แทน JWT ที่หมดอายุ 24 ชม.
--              ออก/ดูคีย์ได้เฉพาะ Dev — คีย์ active ได้ไม่เกิน 1 แถว (ออกใหม่ = ปิดคีย์เดิม)
-- Run order: ไม่มี dependency กับตารางอื่น รันได้ทันที (SQL ก่อน deploy API)
-- Re-run safety: Idempotent — ใช้ CREATE TABLE/INDEX IF NOT EXISTS, รันซ้ำได้ไม่ error
-- =============================================

CREATE TABLE IF NOT EXISTS tbt_label_station_key (
    id              BIGSERIAL NOT NULL,
    device_key      TEXT NOT NULL,
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    create_date     TIMESTAMPTZ NOT NULL,
    create_by       CHARACTER VARYING,
    revoked_date    TIMESTAMPTZ,
    revoked_by      CHARACTER VARYING,
    CONSTRAINT tbt_label_station_key_pk PRIMARY KEY (id)
);

-- is_active = true ได้ไม่เกิน 1 แถว
CREATE UNIQUE INDEX IF NOT EXISTS ux_tbt_label_station_key_active
    ON tbt_label_station_key (is_active) WHERE is_active = TRUE;

-- =============================================
-- Rollback (รันเองเมื่อต้องการถอย):
-- DROP TABLE IF EXISTS tbt_label_station_key;
-- =============================================
