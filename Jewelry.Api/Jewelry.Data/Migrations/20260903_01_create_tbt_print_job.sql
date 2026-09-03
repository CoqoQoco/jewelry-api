-- =============================================
-- Migration: Create tbt_print_job
-- Date: 2026-09-03
-- Description: คิวงานพิมพ์ใบเสร็จข้ามอุปกรณ์ — มือถือ (iOS ไม่มี Web Bluetooth)
--              ส่งข้อความใบเสร็จที่สร้างเสร็จแล้วเข้าคิว, คอมที่บูธต่อเครื่องพิมพ์ poll คิวแล้วดึงไปพิมพ์
-- Run order: ไม่มี dependency กับตารางอื่น รันได้ทันที
-- Re-run safety: Idempotent — ใช้ CREATE TABLE/INDEX IF NOT EXISTS, รันซ้ำได้ไม่ error
-- =============================================

-- =============================================
-- 1. tbt_print_job
-- =============================================
CREATE TABLE IF NOT EXISTS tbt_print_job (
    id              BIGSERIAL NOT NULL,
    invoice_number  CHARACTER VARYING NOT NULL,
    payload         TEXT NOT NULL,
    status          CHARACTER VARYING NOT NULL,
    -- Status: PENDING / PRINTING / PRINTED / FAILED
    error_message   TEXT,
    retry_count     INT NOT NULL DEFAULT 0,
    station_id      CHARACTER VARYING,
    claim_token     CHARACTER VARYING,
    create_by       CHARACTER VARYING,
    create_date     TIMESTAMPTZ NOT NULL,
    claimed_date    TIMESTAMPTZ,
    printed_date    TIMESTAMPTZ,
    CONSTRAINT tbt_print_job_pk PRIMARY KEY (id)
);

CREATE INDEX IF NOT EXISTS idx_tbt_print_job_status_id ON tbt_print_job(status, id);
CREATE INDEX IF NOT EXISTS idx_tbt_print_job_create_date ON tbt_print_job(create_date);
