-- =============================================
-- Migration: Fix remaining N/A product_type by mold vote + product name
-- Date: 2026-08-08
-- Description: หลัง 20260808_01-04 รันบน prod สำเร็จ (แก้ต่างหู + สีทอง) ยังเหลือ
--              product_type = 'N/A' อยู่ 1,006 แถวใน tbt_stock_product และ 1,227
--              แถวใน tbt_sku ทั้งหมดเกิดจาก legacy stock.typep เป็นค่าว่าง (ระบบเก่า
--              ไม่ได้กรอกช่องประเภท) → script เดิม (01) ลง N/A ตาม default เพราะไม่มี
--              typep ให้ map แต่ product_name_en ระบุประเภทไว้ครบทุกแถว (ตรวจแล้ว
--              "ไม่มีคำบอกประเภทในชื่อ" = 0 แถว ทั้งสองตาราง) และ cross-check ระหว่าง
--              แม่พิมพ์ (mold) กับชื่อสินค้าแล้วพบว่าตรงกัน 100% ไม่มีแถวไหนขัดกันเลย
--              (297 แถวที่หา mold vote เจอ ตรงกับชื่อทั้งหมด) ไฟล์นี้จึงขยายตรรกะจาก
--              20260808_04 (เดิมทำแค่ตระกูลต่างหู) ให้ครอบทุกประเภทสินค้า: โหวต
--              product_type จากแม่พิมพ์เดียวกันก่อน (แม่พิมพ์เดียวกัน = สินค้าประเภท
--              เดียวกัน) ถ้าหาแม่พิมพ์ไม่เจอค่อย fallback ไปเดาจากชื่อสินค้าด้วย CASE
--              (ตระกูลต่างหูเช็คก่อน RING เสมอ, RING ใช้ regex word-boundary กัน
--              EARRING/STRING/SPRING แมตช์ผิด) ถ้า resolve ไม่ได้ทั้งคู่ (ไม่มีทั้ง
--              แม่พิมพ์และคำบอกประเภทในชื่อ) ปล่อยเป็น N/A ไว้ตามเดิม ไม่ update
--              คาดผล tbt_stock_product: R 636 / P 266 / B 56 / N 48 = 1,006 แถว
--                     → N/A เหลือ 0
--              คาดผล tbt_sku: R 699 / P 289 / B 137 / N 99 / G 3 = 1,227 แถว
--                     → N/A เหลือ 0
-- Run order: 5 (หลัง 20260808_01, 20260808_02, 20260808_03, 20260808_04)
-- Re-run safe: WHERE (product_type = 'N/A' OR product_type IS NULL) + IS DISTINCT FROM — idempotent
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
        WHERE product_type IS NOT NULL
          AND product_type <> 'N/A'
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
        COALESCE(
            mv.product_type,
            CASE
                WHEN upper(coalesce(sp2.product_name_en, '')) LIKE '%EARRING%'
                     OR upper(coalesce(sp2.product_name_en, '')) LIKE '%EARING%' THEN 'E'
                WHEN upper(coalesce(sp2.product_name_en, '')) LIKE '%LOCKET%' THEN 'LK'
                WHEN upper(coalesce(sp2.product_name_en, '')) LIKE '%PENDANT%' THEN 'P'
                WHEN upper(coalesce(sp2.product_name_en, '')) LIKE '%BRACELET%' THEN 'B'
                WHEN upper(coalesce(sp2.product_name_en, '')) LIKE '%NECKLACE%'
                     OR upper(coalesce(sp2.product_name_en, '')) LIKE '%NECKALCE%' THEN 'N'
                WHEN upper(coalesce(sp2.product_name_en, '')) LIKE '%BANGLE%' THEN 'G'
                WHEN upper(coalesce(sp2.product_name_en, '')) LIKE '%BROOCH%' THEN 'T'
                WHEN upper(coalesce(sp2.product_name_en, '')) LIKE '%CHARM%' THEN 'C'
                WHEN upper(coalesce(sp2.product_name_en, '')) LIKE '%CHAIN%' THEN 'CH'
                WHEN upper(coalesce(sp2.product_name_en, '')) LIKE '%BUTTON%'
                     OR upper(coalesce(sp2.product_name_en, '')) LIKE '%BOTTON%' THEN 'V'
                WHEN upper(coalesce(sp2.product_name_en, '')) ~ '(^|[^A-Z])RING' THEN 'R'
                ELSE NULL
            END
        ) AS new_type
    FROM tbt_stock_product sp2
    LEFT JOIN mold_vote mv ON mv.mold_key = upper(btrim(sp2.mold))
    WHERE coalesce(sp2.product_type, 'N/A') = 'N/A'
) AS resolved
JOIN tbm_product_type pt ON pt.code = resolved.new_type
WHERE sp.stock_number = resolved.stock_number
  AND resolved.new_type IS NOT NULL
  AND sp.product_type IS DISTINCT FROM resolved.new_type;

-- =============================================
-- Statement 2: tbt_sku (mold vote ชุดเดียวกัน คำนวณจาก tbt_stock_product
--              เพราะเป็นแหล่งที่ข้อมูลครบกว่า — join ผ่าน upper(btrim(k2.mold)))
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
        WHERE product_type IS NOT NULL
          AND product_type <> 'N/A'
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
        COALESCE(
            mv.product_type,
            CASE
                WHEN upper(coalesce(k2.product_name_en, '')) LIKE '%EARRING%'
                     OR upper(coalesce(k2.product_name_en, '')) LIKE '%EARING%' THEN 'E'
                WHEN upper(coalesce(k2.product_name_en, '')) LIKE '%LOCKET%' THEN 'LK'
                WHEN upper(coalesce(k2.product_name_en, '')) LIKE '%PENDANT%' THEN 'P'
                WHEN upper(coalesce(k2.product_name_en, '')) LIKE '%BRACELET%' THEN 'B'
                WHEN upper(coalesce(k2.product_name_en, '')) LIKE '%NECKLACE%'
                     OR upper(coalesce(k2.product_name_en, '')) LIKE '%NECKALCE%' THEN 'N'
                WHEN upper(coalesce(k2.product_name_en, '')) LIKE '%BANGLE%' THEN 'G'
                WHEN upper(coalesce(k2.product_name_en, '')) LIKE '%BROOCH%' THEN 'T'
                WHEN upper(coalesce(k2.product_name_en, '')) LIKE '%CHARM%' THEN 'C'
                WHEN upper(coalesce(k2.product_name_en, '')) LIKE '%CHAIN%' THEN 'CH'
                WHEN upper(coalesce(k2.product_name_en, '')) LIKE '%BUTTON%'
                     OR upper(coalesce(k2.product_name_en, '')) LIKE '%BOTTON%' THEN 'V'
                WHEN upper(coalesce(k2.product_name_en, '')) ~ '(^|[^A-Z])RING' THEN 'R'
                ELSE NULL
            END
        ) AS new_type
    FROM tbt_sku k2
    LEFT JOIN mold_vote mv ON mv.mold_key = upper(btrim(k2.mold))
    WHERE coalesce(k2.product_type, 'N/A') = 'N/A'
) AS resolved
JOIN tbm_product_type pt ON pt.code = resolved.new_type
WHERE k.sku_code = resolved.sku_code
  AND resolved.new_type IS NOT NULL
  AND k.product_type IS DISTINCT FROM resolved.new_type;

-- =============================================
-- Verify (รันแยกหลัง migrate เพื่อตรวจผล — คาดว่า N/A ต้องหายไปจาก group by)
-- =============================================
-- SELECT product_type, count(*)
-- FROM tbt_stock_product
-- GROUP BY product_type
-- ORDER BY count(*) DESC;
--
-- SELECT product_type, count(*)
-- FROM tbt_sku
-- GROUP BY product_type
-- ORDER BY count(*) DESC;
