-- =============================================
-- Migration: Create tbt_label_print_job
-- Date: 2026-10-30
-- Description: คิวพิมพ์ป้ายบาร์โค้ดวัตถุดิบ (พลอย) ข้ามอุปกรณ์ — แยกจากคิวใบเสร็จ (tbt_print_job)
--              มือถือส่งข้อมูลป้ายเข้าคิว, คอมที่ต่อเครื่องพิมพ์ป้าย poll คิวแล้วดึงไปพิมพ์
-- Run order: ไม่มี dependency กับตารางอื่น รันได้ทันที (SQL ก่อน deploy API)
-- Re-run safety: Idempotent — ใช้ CREATE TABLE/INDEX IF NOT EXISTS, รันซ้ำได้ไม่ error
-- =============================================

CREATE TABLE IF NOT EXISTS tbt_label_print_job (
    id              BIGSERIAL NOT NULL,
    label_type      CHARACTER VARYING NOT NULL,
    -- label_type: GEM (ตอนนี้มีแค่พลอย)
    reference_code  CHARACTER VARYING NOT NULL,
    payload         TEXT NOT NULL,
    copies          INT NOT NULL DEFAULT 1,
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
    is_deleted      BOOLEAN NOT NULL DEFAULT FALSE,
    deleted_by      CHARACTER VARYING,
    deleted_date    TIMESTAMPTZ,
    CONSTRAINT tbt_label_print_job_pk PRIMARY KEY (id)
);

CREATE INDEX IF NOT EXISTS idx_tbt_label_print_job_status_id ON tbt_label_print_job(status, id);
CREATE INDEX IF NOT EXISTS idx_tbt_label_print_job_create_date ON tbt_label_print_job(create_date);
