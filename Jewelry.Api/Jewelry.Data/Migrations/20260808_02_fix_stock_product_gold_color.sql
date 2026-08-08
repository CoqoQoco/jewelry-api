-- =============================================
-- Migration: Fix stock_product.production_type (gold color) from legacy stock
-- Date: 2026-08-08
-- Description: OldStockService.ProducttionType() เดิมเช็คแค่ PG/YG/SI ตัวย่อ
--              แต่ stock.typeg เก็บเป็นคำเต็ม (Gold, Whitegold, Gold (SE), Silver ฯลฯ)
--              → เกือบทุกแถวตกไป default เป็น White Gold ผิด (~11,864 แถวใน prod)
--              ไฟล์นี้ re-map production_type ด้วยลำดับ:
--              1) หา token WG/YG/PG แบบ word-boundary จากชื่อสินค้า (product_name_en) ก่อน
--              2) ถ้าไม่เจอ → fallback ไปดู stock.typeg (WHITE/PINK/ROSE ต้องเช็คก่อน GOLD
--                 เพราะ "Whitegold" มีคำว่า GOLD อยู่ด้วย)
--              ค่าที่ set resolve จาก tbm_gold.name_en ตาม code ไม่ hardcode string
-- Run order: 2 (หลัง 20260808_01)
-- Re-run safe: WHERE ... IS DISTINCT FROM ... — idempotent
-- =============================================

UPDATE tbt_stock_product sp
SET
    production_type = g.name_en,
    update_date = now(),
    update_by = 'MIGRATION-FIX'
FROM (
    SELECT
        sp2.stock_number,
        COALESCE(
            -- 1) token จากชื่อสินค้า (word-boundary จริง ไม่ใช่ substring ธรรมดา)
            CASE
                WHEN upper(coalesce(sp2.product_name_en, '')) ~ '(^|[^A-Z])WG([^A-Z]|$)' THEN 'WG'
                WHEN upper(coalesce(sp2.product_name_en, '')) ~ '(^|[^A-Z])YG([^A-Z]|$)' THEN 'YG'
                WHEN upper(coalesce(sp2.product_name_en, '')) ~ '(^|[^A-Z])PG([^A-Z]|$)' THEN 'PG'
            END,
            -- 2) fallback จาก stock.typeg (WHITE/PINK/ROSE ต้องมาก่อน GOLD)
            CASE
                WHEN upper(coalesce(s.typeg, '')) LIKE '%WHITE%' OR upper(coalesce(s.typeg, '')) LIKE '%WG%' THEN 'WG'
                WHEN upper(coalesce(s.typeg, '')) LIKE '%PINK%' OR upper(coalesce(s.typeg, '')) LIKE '%ROSE%' OR upper(coalesce(s.typeg, '')) LIKE '%PG%' THEN 'PG'
                WHEN upper(coalesce(s.typeg, '')) LIKE '%SIL%' THEN 'SV'
                WHEN upper(coalesce(s.typeg, '')) LIKE '%GOLD%' OR upper(coalesce(s.typeg, '')) LIKE '%YG%' OR upper(btrim(coalesce(s.typeg, ''))) = 'G' THEN 'YG'
            END
        ) AS new_code
    FROM tbt_stock_product sp2
    LEFT JOIN stock s ON s.noproduct = sp2.product_code
    WHERE sp2.receipt_type = 'transfer'
) AS mapped
JOIN tbm_gold g ON g.code = mapped.new_code
WHERE sp.stock_number = mapped.stock_number
  AND sp.receipt_type = 'transfer'
  AND mapped.new_code IS NOT NULL
  AND sp.production_type IS DISTINCT FROM g.name_en;

-- =============================================
-- Verify (รันแยกหลัง migrate เพื่อตรวจผล — คาดว่ากระทบ ~11,864 แถว
-- 11,223 -> Yellow Gold, 640 -> Pink Gold, 1 -> White Gold)
-- =============================================
-- SELECT production_type, count(*)
-- FROM tbt_stock_product
-- WHERE receipt_type = 'transfer'
-- GROUP BY production_type
-- ORDER BY count(*) DESC;
