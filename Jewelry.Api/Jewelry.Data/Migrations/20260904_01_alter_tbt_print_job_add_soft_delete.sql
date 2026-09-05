-- =============================================
-- Migration: Add soft delete to tbt_print_job
-- Date: 2026-09-04
-- Description: เพิ่ม soft delete ให้คิวงานพิมพ์ — ลบผ่าน UI จะไม่ลบจริง เพื่อเก็บประวัติการพิมพ์
-- Run order: รันหลัง 20260903_01_create_tbt_print_job.sql
-- Re-run safety: Idempotent — ใช้ ADD COLUMN/CREATE INDEX IF NOT EXISTS, รันซ้ำได้ไม่ error
-- =============================================

ALTER TABLE tbt_print_job
    ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    ADD COLUMN IF NOT EXISTS deleted_by CHARACTER VARYING,
    ADD COLUMN IF NOT EXISTS deleted_date TIMESTAMPTZ;

CREATE INDEX IF NOT EXISTS idx_tbt_print_job_deleted_status_id ON tbt_print_job(is_deleted, status, id);
