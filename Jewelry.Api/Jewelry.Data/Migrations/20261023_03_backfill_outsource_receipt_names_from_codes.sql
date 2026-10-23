-- =============================================
-- Migration: TK202610230007 - แก้ข้อมูลสินค้ารับจากร้านนอก (outsource) ที่เก็บ master code แทนชื่อ
-- Date: 2026-10-23
-- Description:
--   1. tbt_sku / tbt_stock_product_receipt_item: production_type (WG/YG/SV...) -> tbm_gold.name_en,
--      production_type_size (7/9/...) -> tbm_gold_size.name_en, tbt_sku.mold = mold_design เมื่อ mold ว่าง
--   2. tbt_stock_piece_material (ทอง/เงินของ outsource): weight_unit 'ct.' -> 'g.'
--   type_barcode ไม่แตะ (สร้างจาก client, รูปแบบไม่แน่นอน) - มี preview ให้ดูเท่านั้น
-- Run: DBeaver บน prod ทีละ step - ดู preview (STEP 1) ก่อน แล้วค่อยรัน STEP 2
-- Rollback: 20261023_03_backfill_outsource_receipt_names_from_codes_rollback.sql (ใช้ตาราง bak_tk202610230007_*)
-- =============================================

-- =============================================
-- STEP 1: PREVIEW (read-only)
-- =============================================

-- 1.1 SKU ที่จะถูกแก้ (คาด ~18)
SELECT s.sku_code, s.production_type, g.name_en AS new_production_type,
       s.production_type_size, gs.name_en AS new_production_type_size,
       s.mold, s.mold_design
FROM tbt_sku s
LEFT JOIN tbm_gold g       ON upper(btrim(s.production_type)) = upper(btrim(g.code))
LEFT JOIN tbm_gold_size gs ON upper(btrim(s.production_type_size)) = upper(btrim(gs.code))
WHERE s.sku_code IN (SELECT p.sku_code FROM tbt_stock_piece p WHERE p.receipt_type = 'outsource')
  AND (g.code IS NOT NULL OR gs.code IS NOT NULL OR (s.mold IS NULL AND s.mold_design IS NOT NULL))
ORDER BY s.sku_code;

-- 1.2 receipt item ที่จะถูกแก้ (คาด ~19)
SELECT i.stock_number, i.production_type, g.name_en AS new_production_type,
       i.production_type_size, gs.name_en AS new_production_type_size
FROM tbt_stock_product_receipt_item i
LEFT JOIN tbm_gold g       ON upper(btrim(i.production_type)) = upper(btrim(g.code))
LEFT JOIN tbm_gold_size gs ON upper(btrim(i.production_type_size)) = upper(btrim(gs.code))
WHERE i.type = 'outsource'
  AND (g.code IS NOT NULL OR gs.code IS NOT NULL)
ORDER BY i.stock_number;

-- 1.3 material ที่จะถูกแก้ (ct. -> g.)
SELECT m.stock_number, m.type, m.weight, m.weight_unit, m.type_barcode
FROM tbt_stock_piece_material m
WHERE m.stock_number IN (SELECT p.stock_number FROM tbt_stock_piece p WHERE p.receipt_type = 'outsource')
  AND m.type IN ('Gold', 'Silver')
  AND m.weight_unit = 'ct.'
ORDER BY m.stock_number;

-- 1.4 type_barcode ของ material ข้างบนที่ยังมี 'ct.' (ไม่แก้อัตโนมัติ - ดูเพื่อตัดสินใจเอง)
SELECT m.stock_number, m.type_barcode
FROM tbt_stock_piece_material m
WHERE m.stock_number IN (SELECT p.stock_number FROM tbt_stock_piece p WHERE p.receipt_type = 'outsource')
  AND m.type IN ('Gold', 'Silver')
  AND m.type_barcode LIKE '%ct.%';

-- =============================================
-- STEP 2: APPLY
-- =============================================
BEGIN;

-- 2.0 สำรองข้อมูลเดิมไว้ใช้ rollback
CREATE TABLE IF NOT EXISTS bak_tk202610230007_sku AS
SELECT sku_code, production_type, production_type_size, mold, update_date, update_by
FROM tbt_sku
WHERE sku_code IN (SELECT p.sku_code FROM tbt_stock_piece p WHERE p.receipt_type = 'outsource');

CREATE TABLE IF NOT EXISTS bak_tk202610230007_receipt_item AS
SELECT i.stock_receipt_number, i.production_type, i.production_type_size, i.update_date, i.update_by
FROM tbt_stock_product_receipt_item i
WHERE i.type = 'outsource';

CREATE TABLE IF NOT EXISTS bak_tk202610230007_material AS
SELECT m.id, m.weight_unit, m.update_date, m.update_by
FROM tbt_stock_piece_material m
WHERE m.stock_number IN (SELECT p.stock_number FROM tbt_stock_piece p WHERE p.receipt_type = 'outsource')
  AND m.type IN ('Gold', 'Silver')
  AND m.weight_unit = 'ct.';

-- 2.1 tbt_sku
UPDATE tbt_sku s
SET production_type      = COALESCE((SELECT g.name_en FROM tbm_gold g
                                     WHERE upper(btrim(g.code)) = upper(btrim(s.production_type)) LIMIT 1), s.production_type),
    production_type_size = COALESCE((SELECT gs.name_en FROM tbm_gold_size gs
                                     WHERE upper(btrim(gs.code)) = upper(btrim(s.production_type_size)) LIMIT 1), s.production_type_size),
    mold                 = COALESCE(s.mold, s.mold_design),
    update_date          = now(),
    update_by            = 'TK202610230007'
WHERE s.sku_code IN (SELECT p.sku_code FROM tbt_stock_piece p WHERE p.receipt_type = 'outsource')
  AND (EXISTS (SELECT 1 FROM tbm_gold g WHERE upper(btrim(g.code)) = upper(btrim(s.production_type)))
       OR EXISTS (SELECT 1 FROM tbm_gold_size gs WHERE upper(btrim(gs.code)) = upper(btrim(s.production_type_size)))
       OR (s.mold IS NULL AND s.mold_design IS NOT NULL));

-- 2.2 tbt_stock_product_receipt_item
UPDATE tbt_stock_product_receipt_item i
SET production_type      = COALESCE((SELECT g.name_en FROM tbm_gold g
                                     WHERE upper(btrim(g.code)) = upper(btrim(i.production_type)) LIMIT 1), i.production_type),
    production_type_size = COALESCE((SELECT gs.name_en FROM tbm_gold_size gs
                                     WHERE upper(btrim(gs.code)) = upper(btrim(i.production_type_size)) LIMIT 1), i.production_type_size),
    update_date          = now(),
    update_by            = 'TK202610230007'
WHERE i.type = 'outsource'
  AND (EXISTS (SELECT 1 FROM tbm_gold g WHERE upper(btrim(g.code)) = upper(btrim(i.production_type)))
       OR EXISTS (SELECT 1 FROM tbm_gold_size gs WHERE upper(btrim(gs.code)) = upper(btrim(i.production_type_size))));

-- 2.3 tbt_stock_piece_material
UPDATE tbt_stock_piece_material m
SET weight_unit = 'g.',
    update_date = now(),
    update_by   = 'TK202610230007'
WHERE m.stock_number IN (SELECT p.stock_number FROM tbt_stock_piece p WHERE p.receipt_type = 'outsource')
  AND m.type IN ('Gold', 'Silver')
  AND m.weight_unit = 'ct.';

COMMIT;

-- =============================================
-- STEP 3: VERIFY (ควรได้ 0 แถวทุกข้อ)
-- =============================================
SELECT s.sku_code, s.production_type, s.production_type_size, s.mold
FROM tbt_sku s
WHERE s.sku_code IN (SELECT p.sku_code FROM tbt_stock_piece p WHERE p.receipt_type = 'outsource')
  AND (EXISTS (SELECT 1 FROM tbm_gold g WHERE upper(btrim(g.code)) = upper(btrim(s.production_type)))
       OR EXISTS (SELECT 1 FROM tbm_gold_size gs WHERE upper(btrim(gs.code)) = upper(btrim(s.production_type_size)))
       OR (s.mold IS NULL AND s.mold_design IS NOT NULL));

SELECT i.stock_number, i.production_type, i.production_type_size
FROM tbt_stock_product_receipt_item i
WHERE i.type = 'outsource'
  AND (EXISTS (SELECT 1 FROM tbm_gold g WHERE upper(btrim(g.code)) = upper(btrim(i.production_type)))
       OR EXISTS (SELECT 1 FROM tbm_gold_size gs WHERE upper(btrim(gs.code)) = upper(btrim(i.production_type_size))));

SELECT m.stock_number, m.type, m.weight_unit
FROM tbt_stock_piece_material m
WHERE m.stock_number IN (SELECT p.stock_number FROM tbt_stock_piece p WHERE p.receipt_type = 'outsource')
  AND m.type IN ('Gold', 'Silver')
  AND m.weight_unit = 'ct.';

-- ดูผลลัพธ์จริง
SELECT s.production_type, s.production_type_size, count(*)
FROM tbt_sku s
WHERE s.sku_code IN (SELECT p.sku_code FROM tbt_stock_piece p WHERE p.receipt_type = 'outsource')
GROUP BY 1, 2 ORDER BY 1, 2;
