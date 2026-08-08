-- =============================================
-- Migration: Fix stock_product.product_type from legacy stock (typep mis-mapped)
-- Date: 2026-08-08
-- Description: OldStockService.GetProductType() เดิมเช็ค "RING" ก่อน "EARRING"
--              ทำให้สินค้าต่างหูทั้งหมดถูก map เป็นแหวน (R) ผิด (~3,180 แถวใน prod)
--              ไฟล์นี้ re-map product_type + product_type_name จาก stock.typep
--              ด้วยลำดับเงื่อนไขที่แก้แล้ว (ตระกูลต่างหูมาก่อน RING เสมอ)
--              ส่วนที่ 2: เดา product_type จากชื่อสินค้าสำหรับแถวที่หา record
--              ต้นทางใน stock ไม่เจอ (~476 แถว) แต่ชื่อมีคำว่า EARRING/EARING
-- Run order: 1 (ก่อน 20260808_02, 20260808_03)
-- Re-run safe: WHERE ... IS DISTINCT FROM ... — idempotent
-- =============================================

-- =============================================
-- Part 1: Re-map จาก stock.typep (join ผ่าน product_code = stock.noproduct)
-- =============================================
UPDATE tbt_stock_product sp
SET
    product_type = mapped.new_code,
    product_type_name = pt.name_th,
    update_date = now(),
    update_by = 'MIGRATION-FIX'
FROM (
    SELECT
        sp2.stock_number,
        CASE
            -- ตระกูลต่างหูต้องเช็คก่อน RING เสมอ (EARRING มีคำว่า RING อยู่ในตัว)
            WHEN upper(btrim(s.typep)) LIKE '%EARRI%' THEN
                CASE
                    WHEN upper(btrim(s.typep)) LIKE '%STUD%' OR upper(btrim(s.typep)) LIKE '%STDU%' THEN 'ES'
                    WHEN upper(btrim(s.typep)) LIKE '%HOOK%' THEN 'EH'
                    WHEN upper(btrim(s.typep)) LIKE '%LOCK%' THEN 'EL'
                    ELSE 'E'
                END
            WHEN upper(btrim(s.typep)) LIKE '%LOCKET%' THEN 'LK'
            WHEN upper(btrim(s.typep)) LIKE '%PENDANT%' THEN 'P'
            WHEN upper(btrim(s.typep)) LIKE '%RING%' THEN 'R'
            WHEN upper(btrim(s.typep)) LIKE '%BRACELET%' THEN 'B'
            WHEN upper(btrim(s.typep)) LIKE '%NECKLACE%' OR upper(btrim(s.typep)) LIKE '%NECKALCE%' THEN 'N'
            WHEN upper(btrim(s.typep)) LIKE '%BANGLE%' THEN 'G'
            WHEN upper(btrim(s.typep)) LIKE '%BROOCH%' THEN 'T'
            WHEN upper(btrim(s.typep)) LIKE '%CHARM%' THEN 'C'
            WHEN upper(btrim(s.typep)) LIKE '%BUTTON%' OR upper(btrim(s.typep)) LIKE '%BOTTON%' THEN 'V'
            WHEN upper(btrim(s.typep)) LIKE '%CHAIN%' THEN 'CH'
            ELSE NULL
        END AS new_code
    FROM tbt_stock_product sp2
    JOIN stock s ON s.noproduct = sp2.product_code
    WHERE sp2.receipt_type = 'transfer'
) AS mapped
JOIN tbm_product_type pt ON pt.code = mapped.new_code
WHERE sp.stock_number = mapped.stock_number
  AND sp.receipt_type = 'transfer'
  AND mapped.new_code IS NOT NULL
  AND sp.product_type IS DISTINCT FROM mapped.new_code;

-- =============================================
-- Part 2: เดาจากชื่อสินค้า สำหรับแถวที่ไม่มี record ต้นทางใน stock
--         (ไม่มีทาง map จาก typep ได้ — ใช้ชื่อสินค้าแทนเป็นข้อมูลสำรอง)
-- =============================================
UPDATE tbt_stock_product sp
SET
    product_type = 'E',
    product_type_name = pt.name_th,
    update_date = now(),
    update_by = 'MIGRATION-FIX'
FROM tbm_product_type pt
WHERE pt.code = 'E'
  AND sp.receipt_type = 'transfer'
  AND NOT EXISTS (SELECT 1 FROM stock s WHERE s.noproduct = sp.product_code)
  AND (upper(sp.product_name_en) LIKE '%EARRING%' OR upper(sp.product_name_en) LIKE '%EARING%')
  AND (sp.product_type IS NULL OR sp.product_type NOT IN ('E', 'ES', 'EL', 'EH'))
  AND sp.product_type IS DISTINCT FROM 'E';

-- =============================================
-- Verify (รันแยกหลัง migrate เพื่อตรวจผล)
-- =============================================
-- SELECT product_type, product_type_name, count(*)
-- FROM tbt_stock_product
-- WHERE receipt_type = 'transfer'
-- GROUP BY product_type, product_type_name
-- ORDER BY count(*) DESC;
