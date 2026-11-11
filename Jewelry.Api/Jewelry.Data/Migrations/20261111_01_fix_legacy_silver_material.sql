-- =============================================
-- Migration: Fix legacy silver material rows (DK-SIL%)   [TK202611110001]
-- Date: 2026-11-11
-- Description:
--   สต็อกเงินที่โอนจากระบบเก่า เก็บแถวโลหะเงินเป็น type='Gem', type_code ว่าง, qty=0
--   (type_origin ขึ้นต้น 'Sil', weight_unit 'g.') -> แก้เป็น type='Silver', type_code='SV', qty=1,
--   type_barcode = '<weight> g. Silver' เหมือนสต็อกเงินใหม่
--   (a) แถวเงิน  (b) แถวพลอยที่ type_code ว่าง ให้เอาชื่อจาก type_origin มาใส่ type_code (ไม่แตะ type_barcode)
--
-- วิธีรัน: DBeaver ปิด auto-commit -> รัน PREVIEW แยก -> รันตั้งแต่ BEGIN ถึง VERIFY -> ตรวจผล VERIFY
--   แล้วรัน COMMIT; (หรือ ROLLBACK;) ด้วยมือ  (สคริปต์ไม่ COMMIT ให้เอง)
-- ลำดับ deploy: UI ก่อน (หน้าเอกสารขายต้องรองรับ type 'Silver') -> รัน SQL นี้ -> API
-- Re-run safety: ไม่ idempotent ในส่วน BACKUP (CREATE TABLE จะ error ถ้ามีอยู่แล้ว = กันรันซ้ำ)
-- Rollback: 20261111_01_rollback_fix_legacy_silver_material.sql
--   (b) ไม่แตะแถวที่ type_origin ขึ้นต้น 'Sil' ทุกกรณี (เช่น DK-SIL-20H-8290 หน่วย ct.)
-- ตาราง tbt_stock_product_material: prod ไม่มีแถว DK-SIL% จึงไม่แตะ
-- =============================================

-- =============================================
-- 0. PREVIEW (รันแยกก่อน แล้วตรวจตัวเลข/ชื่อพลอย)
-- =============================================
SELECT count(*) AS silver_rows_to_change
FROM tbt_stock_piece_material
WHERE stock_number LIKE 'DK-SIL%' AND lower(type) = 'gem'
  AND trim(coalesce(type_code, '')) = ''
  AND trim(coalesce(type_origin, '')) ILIKE 'sil%'
  AND trim(coalesce(weight_unit, '')) ILIKE 'g%';

SELECT count(*) AS blank_code_gem_rows_to_change
FROM tbt_stock_piece_material
WHERE stock_number LIKE 'DK-SIL%'
  AND trim(coalesce(type_code, '')) = ''
  AND trim(split_part(trim(coalesce(type_origin, '')), '(', 1)) <> ''
  AND lower(type) = 'gem' AND trim(coalesce(type_origin, '')) NOT ILIKE 'sil%';

SELECT trim(type_origin) AS type_origin_raw, trim(split_part(trim(coalesce(type_origin, '')), '(', 1)) AS type_code_new, count(*)
FROM tbt_stock_piece_material
WHERE stock_number LIKE 'DK-SIL%'
  AND trim(coalesce(type_code, '')) = ''
  AND trim(split_part(trim(coalesce(type_origin, '')), '(', 1)) <> ''
  AND lower(type) = 'gem' AND trim(coalesce(type_origin, '')) NOT ILIKE 'sil%'
GROUP BY trim(type_origin), trim(split_part(trim(coalesce(type_origin, '')), '(', 1))
ORDER BY count(*) DESC;

BEGIN;

-- =============================================
-- 1. BACKUP
-- =============================================
CREATE TABLE bak_20261111_silver_material AS
SELECT * FROM tbt_stock_piece_material
WHERE stock_number LIKE 'DK-SIL%'
  AND trim(coalesce(type_code, '')) = ''
  AND (
        (trim(split_part(trim(coalesce(type_origin, '')), '(', 1)) <> '' AND lower(type) = 'gem' AND trim(coalesce(type_origin, '')) NOT ILIKE 'sil%')
     OR (lower(type) = 'gem' AND trim(coalesce(type_origin, '')) ILIKE 'sil%' AND trim(coalesce(weight_unit, '')) ILIKE 'g%')
  );

SELECT count(*) AS backed_up FROM bak_20261111_silver_material;

-- =============================================
-- 2. UPDATE (a) แถวเงิน
-- =============================================
UPDATE tbt_stock_piece_material
SET type = 'Silver',
    type_code = 'SV',
    qty = CASE WHEN coalesce(qty, 0) = 0 THEN 1 ELSE qty END,
    type_barcode = trim_scale(coalesce(weight, 0))::text || ' ' || trim(weight_unit) || ' Silver',
    update_date = now(),
    update_by = 'TK202611110001'
WHERE stock_number LIKE 'DK-SIL%' AND lower(type) = 'gem'
  AND trim(coalesce(type_code, '')) = ''
  AND trim(coalesce(type_origin, '')) ILIKE 'sil%'
  AND trim(coalesce(weight_unit, '')) ILIKE 'g%';

-- =============================================
-- 3. UPDATE (b) พลอย type_code ว่าง -> ใช้ type_origin (type_barcode คงเดิม)
-- =============================================
UPDATE tbt_stock_piece_material
SET type_code = trim(split_part(trim(coalesce(type_origin, '')), '(', 1)),
    update_date = now(),
    update_by = 'TK202611110001'
WHERE stock_number LIKE 'DK-SIL%'
  AND trim(coalesce(type_code, '')) = ''
  AND trim(split_part(trim(coalesce(type_origin, '')), '(', 1)) <> ''
  AND lower(type) = 'gem' AND trim(coalesce(type_origin, '')) NOT ILIKE 'sil%';

-- =============================================
-- 4. VERIFY (ก่อน COMMIT: ตัวเลขต้องตรง PREVIEW, remaining = 0)
-- =============================================
SELECT type, type_code, count(*), min(qty) AS min_qty, min(type_barcode) AS sample_barcode
FROM tbt_stock_piece_material
WHERE update_by = 'TK202611110001'
GROUP BY type, type_code
ORDER BY count(*) DESC
LIMIT 30;

SELECT count(*) AS remaining_blank_code
FROM tbt_stock_piece_material
WHERE stock_number LIKE 'DK-SIL%' AND trim(coalesce(type_code, '')) = '' AND trim(coalesce(type_origin, '')) <> '' AND lower(type) = 'gem' AND trim(type_origin) NOT ILIKE 'sil%';

-- COMMIT;
-- ROLLBACK;
