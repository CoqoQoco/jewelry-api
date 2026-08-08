-- =============================================
-- Migration: Fix remaining EARRING rows not yet classified by mold
-- Date: 2026-08-08
-- Description: หลัง 20260808_01-03 ยังเหลือแถวที่ชื่อสินค้ามีคำว่า EARRING/EARING
--              แต่ product_type ยังไม่ใช่ตระกูลต่างหู (tbt_stock_product 131 แถว
--              = 112 แถว legacy stock.typep ว่าง + 19 แถว legacy บอกคนละอย่างกับชื่อ,
--              tbt_sku ~251 แถว)
--              ตรวจสอบ 19 แถวที่ขัดกันแล้วพบว่า legacy ผิด ชื่อถูก (product_number +
--              mold ขึ้นต้น EF ซึ่งเป็น prefix ตระกูลต่างหู, size เป็น 0/#/ว่าง ไม่มี
--              ไซส์แหวน) → ไฟล์นี้ให้ชื่อสินค้าชนะทุกกรณี ไม่เช็ค stock.typep เลย
--              ชื่อสินค้าบอกได้แค่ระดับ "ต่างหู" (กลุ่มนี้ไม่มีคำว่า STUD/HOOK/LOCK
--              เลยสักแถว) จึงใช้ mold (แม่พิมพ์) เป็นตัวระบุประเภทย่อยก่อนเสมอ —
--              แม่พิมพ์เดียวกันย่อมเป็นสินค้าประเภทเดียวกัน โดยโหวตจาก
--              tbt_stock_product ที่มี product_type เป็นตระกูลต่างหูชัดเจนอยู่แล้ว
--              (E/ES/EL/EH) เลือก product_type ที่พบบ่อยที่สุดต่อ 1 แม่พิมพ์
--              (tie-break ด้วย product_type ให้ deterministic) ถ้าหาแม่พิมพ์ไม่เจอ
--              fallback เป็น 'E' (ต่างหูทั่วไป)
--              คาดผล tbt_stock_product: ES 49 / EL 7 / E 75 = 131 แถว
--                     tbt_sku: ~251 แถว
-- Run order: 4 (หลัง 20260808_01, 20260808_02, 20260808_03)
-- Re-run safe: WHERE ... IS DISTINCT FROM ... + product_type NOT IN ('E','ES','EL','EH','SE') — idempotent
-- =============================================

-- =============================================
-- Statement 1: tbt_stock_product
-- =============================================
WITH mold_vote AS (
    SELECT mold_key, product_type
    FROM (
        SELECT
            upper(btrim(mold)) AS mold_key,
            product_type,
            row_number() OVER (
                PARTITION BY upper(btrim(mold))
                ORDER BY count(*) DESC, product_type
            ) AS rn
        FROM tbt_stock_product
        WHERE product_type IN ('E', 'ES', 'EL', 'EH')
          AND NULLIF(btrim(mold), '') IS NOT NULL
        GROUP BY upper(btrim(mold)), product_type
    ) ranked
    WHERE rn = 1
)
UPDATE tbt_stock_product sp
SET
    product_type = resolved.new_type,
    product_type_name = pt.name_th,
    update_date = now(),
    update_by = 'MIGRATION-FIX'
FROM (
    SELECT
        sp2.stock_number,
        COALESCE(mv.product_type, 'E') AS new_type
    FROM tbt_stock_product sp2
    LEFT JOIN mold_vote mv ON mv.mold_key = upper(btrim(sp2.mold))
    WHERE (upper(coalesce(sp2.product_name_en, '')) LIKE '%EARRING%' OR upper(coalesce(sp2.product_name_en, '')) LIKE '%EARING%')
      AND coalesce(sp2.product_type, '') NOT IN ('E', 'ES', 'EL', 'EH', 'SE')
) AS resolved
JOIN tbm_product_type pt ON pt.code = resolved.new_type
WHERE sp.stock_number = resolved.stock_number
  AND sp.product_type IS DISTINCT FROM resolved.new_type;

-- =============================================
-- Statement 2: tbt_sku (mold vote ชุดเดียวกัน อ้างอิงจาก tbt_stock_product
--              เพราะเป็นแหล่งที่ข้อมูลครบกว่า — join ผ่าน upper(btrim(k.mold)))
-- =============================================
WITH mold_vote AS (
    SELECT mold_key, product_type
    FROM (
        SELECT
            upper(btrim(mold)) AS mold_key,
            product_type,
            row_number() OVER (
                PARTITION BY upper(btrim(mold))
                ORDER BY count(*) DESC, product_type
            ) AS rn
        FROM tbt_stock_product
        WHERE product_type IN ('E', 'ES', 'EL', 'EH')
          AND NULLIF(btrim(mold), '') IS NOT NULL
        GROUP BY upper(btrim(mold)), product_type
    ) ranked
    WHERE rn = 1
)
UPDATE tbt_sku k
SET
    product_type = resolved.new_type,
    product_type_name = pt.name_th,
    update_date = now(),
    update_by = 'MIGRATION-FIX'
FROM (
    SELECT
        k2.sku_code,
        COALESCE(mv.product_type, 'E') AS new_type
    FROM tbt_sku k2
    LEFT JOIN mold_vote mv ON mv.mold_key = upper(btrim(k2.mold))
    WHERE (upper(coalesce(k2.product_name_en, '')) LIKE '%EARRING%' OR upper(coalesce(k2.product_name_en, '')) LIKE '%EARING%')
      AND coalesce(k2.product_type, '') NOT IN ('E', 'ES', 'EL', 'EH', 'SE')
) AS resolved
JOIN tbm_product_type pt ON pt.code = resolved.new_type
WHERE k.sku_code = resolved.sku_code
  AND k.product_type IS DISTINCT FROM resolved.new_type;

-- =============================================
-- Verify (รันแยกหลัง migrate เพื่อตรวจผล)
-- =============================================
-- SELECT product_type, count(*)
-- FROM tbt_stock_product
-- WHERE (upper(coalesce(product_name_en, '')) LIKE '%EARRING%' OR upper(coalesce(product_name_en, '')) LIKE '%EARING%')
-- GROUP BY product_type
-- ORDER BY count(*) DESC;
--
-- SELECT product_type, count(*)
-- FROM tbt_sku
-- WHERE (upper(coalesce(product_name_en, '')) LIKE '%EARRING%' OR upper(coalesce(product_name_en, '')) LIKE '%EARING%')
-- GROUP BY product_type
-- ORDER BY count(*) DESC;
