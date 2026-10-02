-- =============================================
-- Migration: tbm_worker — เพิ่ม employment_type (ประเภทการจ้าง)
-- Date: 2026-10-01
-- Description: แยกช่างในบ้าน/นอกบ้าน/ร้านรับช่วง เพื่อใช้คำนวณสัดส่วนค่าแรงจ้างนอก (Production Insight: ช่างและค่าแรง)
-- Re-run safety: Idempotent — ADD COLUMN IF NOT EXISTS, constraint DROP+ADD, backfill UPDATE ทั้งหมดมี
--   WHERE employment_type IS NULL (รันซ้ำไม่ทับค่าที่ตั้งไว้แล้ว ไม่ว่าจะมาจาก backfill รอบนี้หรือจากการแก้ไขผ่าน
--   WorkerController/Update ภายหลัง)
-- =============================================

ALTER TABLE tbm_worker
    ADD COLUMN IF NOT EXISTS employment_type CHARACTER VARYING;

ALTER TABLE tbm_worker
    DROP CONSTRAINT IF EXISTS tbm_worker_employment_type_ck;
ALTER TABLE tbm_worker
    ADD CONSTRAINT tbm_worker_employment_type_ck CHECK (employment_type IS NULL OR employment_type IN ('IN_HOUSE', 'OUTSIDE', 'SHOP'));

-- รหัส placeholder ("รอจ่าย...") และรหัส/ชื่อทดสอบ (TEST) ไม่ใช่ช่างจริง — ปล่อย NULL ไว้ (ไม่จัดประเภทการจ้าง)
-- ส่วนที่เหลือจัดตาม keyword ใน name_th: "นอกบ้าน" = OUTSIDE, "ร้าน" = SHOP, อื่นๆ = IN_HOUSE (ค่าเริ่มต้น)
UPDATE tbm_worker
SET employment_type = 'OUTSIDE'
WHERE employment_type IS NULL
  AND name_th NOT LIKE 'รอ%'
  AND name_th NOT ILIKE '%TEST%'
  AND name_th LIKE '%นอกบ้าน%';

UPDATE tbm_worker
SET employment_type = 'SHOP'
WHERE employment_type IS NULL
  AND name_th NOT LIKE 'รอ%'
  AND name_th NOT ILIKE '%TEST%'
  AND name_th LIKE '%ร้าน%';

UPDATE tbm_worker
SET employment_type = 'IN_HOUSE'
WHERE employment_type IS NULL
  AND name_th NOT LIKE 'รอ%'
  AND name_th NOT ILIKE '%TEST%';

-- =============================================
-- Verification (uncomment to run manually)
-- =============================================
-- SELECT employment_type, count(*) FROM tbm_worker GROUP BY employment_type ORDER BY employment_type;
-- SELECT code, name_th, employment_type FROM tbm_worker WHERE employment_type IS NULL;
