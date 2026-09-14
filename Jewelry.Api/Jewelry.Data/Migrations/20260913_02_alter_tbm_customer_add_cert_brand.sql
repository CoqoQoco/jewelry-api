-- =============================================
-- Migration: Add cert_brand_name / cert_logo_path to tbm_customer
-- Date: 2026-09-13
-- Description: เก็บค่า default โลโก้/ชื่อแบรนด์ที่ลูกค้าเลือกไว้สำหรับพิมพ์ใบรับรองสินค้า
--              (ตั้งครั้งเดียวผ่าน Certificate/Create ที่ saveBrandAsCustomerDefault=true แล้วใช้ซ้ำได้ทุกใบถัดไป)
-- Run order: ไม่มี dependency กับตารางอื่น รันได้ทันที
-- Re-run safety: Idempotent — ADD COLUMN IF NOT EXISTS
-- =============================================

ALTER TABLE tbm_customer
    ADD COLUMN IF NOT EXISTS cert_brand_name CHARACTER VARYING,
    ADD COLUMN IF NOT EXISTS cert_logo_path CHARACTER VARYING;

-- =============================================
-- Rollback:
-- ALTER TABLE tbm_customer DROP COLUMN IF EXISTS cert_logo_path;
-- ALTER TABLE tbm_customer DROP COLUMN IF EXISTS cert_brand_name;
-- =============================================
