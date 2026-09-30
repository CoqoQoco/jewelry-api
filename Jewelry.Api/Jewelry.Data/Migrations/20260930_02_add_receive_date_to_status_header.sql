-- =============================================
-- Migration: เพิ่ม receive_date บน tbt_production_plan_status_header
-- Date: 2026-09-30
-- Description: บันทึกเวลาที่แผน "พ้นสถานะรอ (wait)" เข้าสถานะทำงานจริง (work) — ก่อนหน้านี้ status
--   ที่เป็น "รอ" (49/59/69/79/89/94) ไม่เคยถูกบันทึกเป็น header แยกเลย (header สร้างด้วย Status=สถานะทำงาน
--   ทันทีตอนโอนงาน) ทำให้แยก wait/work time ไม่ได้ — คอลัมน์นี้เติมส่วนที่ขาด โดยไม่แก้ schema เดิม
-- ⚠️ ต้อง run migration นี้ "ก่อน" deploy API (EF map คอลัมน์นี้ไว้แล้ว รันช้าจะทำให้ API พังตอน query/save header)
-- No backfill — ข้อมูลเก่าจะเป็น NULL ทั้งหมด (แยก wait/work ย้อนหลังไม่ได้ ตามที่ตรวจสอบแล้วว่าข้อมูลไม่พอ)
-- Re-run safety: Idempotent — ADD COLUMN IF NOT EXISTS
-- =============================================

ALTER TABLE tbt_production_plan_status_header
    ADD COLUMN IF NOT EXISTS receive_date TIMESTAMPTZ NULL;
