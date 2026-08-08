-- =============================================
-- Migration: Fix SKU (product_type / production_type) from Stock Product
-- Date: 2026-08-08
-- Description: sync tbt_sku.product_type / product_type_name / production_type
--              จาก 3 แหล่งเรียงตามลำดับความน่าเชื่อถือของข้อมูลต้นทาง:
--              Part 1 — sync จาก tbt_stock_product หลังแก้ที่ 20260808_01 + 20260808_02
--                       derive sku_code ด้วยสูตรเดียวกับ backfill_sku_from_stock_product.sql
--                       (product_number ถ้ามี → 'SKU-' || upper(btrim(product_number))
--                        ไม่มี → MD5 hash จาก product_name_th + mold + size + production_type
--                        + production_type_size) แล้ว aggregate ด้วย MIN() ต่อ sku_code
--              Part 2 — แถวที่ Part 1 แตะไม่ถึง (~3,708 แถว ไม่ได้มาจาก tbt_stock_product เช่น
--                       สร้างโดย create_by = CoqoAdmin) กู้ข้อมูลต้นทางผ่าน
--                       stock.codeproduct = tbt_sku.product_number (~1,672 แถว)
--              Part 3 — แถวที่เหลือไม่มีต้นทางทั้งคู่ (~2,036 แถว) เดาจากชื่อสินค้าอย่างเดียว
-- Run order: 3 (หลัง 20260808_01, 20260808_02)
-- Re-run safe: WHERE เทียบค่าจริงก่อน update — idempotent
-- =============================================

-- =============================================
-- Part 1: Sync จาก tbt_stock_product (ที่แก้แล้วใน 01 + 02)
-- =============================================
WITH derived AS (
    SELECT
        stock_number,
        CASE
            WHEN NULLIF(TRIM(product_number), '') IS NOT NULL
                THEN 'SKU-' || UPPER(TRIM(product_number))
            ELSE
                'SKU-' || SUBSTRING(
                    MD5(
                        LOWER(
                            COALESCE(product_name_th, '') ||
                            COALESCE(mold, '')             ||
                            COALESCE(size, '')             ||
                            COALESCE(production_type, '')  ||
                            COALESCE(production_type_size, '')
                        )
                    ),
                    1, 8
                )
        END AS sku_code
    FROM tbt_stock_product
),
agg AS (
    SELECT
        derived.sku_code,
        MIN(sp.product_type)      AS product_type,
        MIN(sp.product_type_name) AS product_type_name,
        MIN(sp.production_type)   AS production_type
    FROM tbt_stock_product sp
    JOIN derived ON derived.stock_number = sp.stock_number
    GROUP BY derived.sku_code
)
UPDATE tbt_sku k
SET
    product_type = agg.product_type,
    product_type_name = agg.product_type_name,
    production_type = agg.production_type,
    update_date = now(),
    update_by = 'MIGRATION-FIX'
FROM agg
WHERE k.sku_code = agg.sku_code
  AND (
        k.product_type IS DISTINCT FROM agg.product_type
     OR k.product_type_name IS DISTINCT FROM agg.product_type_name
     OR k.production_type IS DISTINCT FROM agg.production_type
  );

-- =============================================
-- Part 2: กู้จาก legacy stock ผ่าน codeproduct (แถวที่ Part 1 แตะไม่ถึง)
--         ตอน insert (path อื่นที่ใช้ mapping เดียวกัน) ProductNumber = stock.Codeproduct
--         → join กลับได้ผ่าน stock.codeproduct = tbt_sku.product_number
--         stock.codeproduct ซ้ำได้ ต้อง GROUP BY + MIN() กันแถวซ้ำก่อน join กลับ tbt_sku
-- =============================================
WITH legacy_map AS (
    SELECT
        upper(btrim(s.codeproduct)) AS codeproduct_key,
        MIN(
            CASE
                -- ตระกูลต่างหูต้องเช็คก่อน RING เสมอ — เงื่อนไขชุดเดียวกับ Part 1 ของไฟล์ 01
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
            END
        ) AS product_type_code,
        MIN(
            -- fallback จาก typeg เมื่อชื่อสินค้าไม่มี token — เงื่อนไขชุดเดียวกับไฟล์ 02
            CASE
                WHEN upper(coalesce(s.typeg, '')) LIKE '%WHITE%' OR upper(coalesce(s.typeg, '')) LIKE '%WG%' THEN 'WG'
                WHEN upper(coalesce(s.typeg, '')) LIKE '%PINK%' OR upper(coalesce(s.typeg, '')) LIKE '%ROSE%' OR upper(coalesce(s.typeg, '')) LIKE '%PG%' THEN 'PG'
                WHEN upper(coalesce(s.typeg, '')) LIKE '%SIL%' THEN 'SV'
                WHEN upper(coalesce(s.typeg, '')) LIKE '%GOLD%' OR upper(coalesce(s.typeg, '')) LIKE '%YG%' OR upper(btrim(coalesce(s.typeg, ''))) = 'G' THEN 'YG'
            END
        ) AS typeg_fallback_code
    FROM stock s
    WHERE NULLIF(btrim(s.codeproduct), '') IS NOT NULL
    GROUP BY upper(btrim(s.codeproduct))
),
sku_target AS (
    SELECT
        k.sku_code,
        lm.product_type_code,
        COALESCE(
            -- token จากชื่อสินค้าก่อน (word-boundary จริง) แล้วค่อย fallback ไป typeg
            CASE
                WHEN upper(coalesce(k.product_name_en, '')) ~ '(^|[^A-Z])WG([^A-Z]|$)' THEN 'WG'
                WHEN upper(coalesce(k.product_name_en, '')) ~ '(^|[^A-Z])YG([^A-Z]|$)' THEN 'YG'
                WHEN upper(coalesce(k.product_name_en, '')) ~ '(^|[^A-Z])PG([^A-Z]|$)' THEN 'PG'
            END,
            lm.typeg_fallback_code
        ) AS production_type_code
    FROM tbt_sku k
    JOIN legacy_map lm ON lm.codeproduct_key = upper(btrim(k.product_number))
    WHERE NOT EXISTS (
        SELECT 1 FROM tbt_stock_product sp
        WHERE 'SKU-' || upper(btrim(sp.product_number)) = k.sku_code
    )
)
UPDATE tbt_sku k
SET
    product_type = COALESCE(st.product_type_code, k.product_type),
    product_type_name = COALESCE(pt.name_th, k.product_type_name),
    production_type = COALESCE(g.name_en, k.production_type),
    update_date = now(),
    update_by = 'MIGRATION-FIX'
FROM sku_target st
LEFT JOIN tbm_product_type pt ON pt.code = st.product_type_code
LEFT JOIN tbm_gold g ON g.code = st.production_type_code
WHERE k.sku_code = st.sku_code
  AND (
        k.product_type IS DISTINCT FROM COALESCE(st.product_type_code, k.product_type)
     OR k.product_type_name IS DISTINCT FROM COALESCE(pt.name_th, k.product_type_name)
     OR k.production_type IS DISTINCT FROM COALESCE(g.name_en, k.production_type)
  );

-- =============================================
-- Part 3: เดาจากชื่อสินค้าอย่างเดียว (แถวที่เหลือ ไม่มีต้นทางทั้ง
--         tbt_stock_product และ stock.codeproduct)
-- =============================================

-- Part 3a: production_type — token WG/YG/PG จาก product_name_en เท่านั้น
--          ไม่มี token → ไม่ update (ปล่อยค่าเดิม)
UPDATE tbt_sku k
SET
    production_type = g.name_en,
    update_date = now(),
    update_by = 'MIGRATION-FIX'
FROM tbm_gold g
WHERE g.code = (
        CASE
            WHEN upper(coalesce(k.product_name_en, '')) ~ '(^|[^A-Z])WG([^A-Z]|$)' THEN 'WG'
            WHEN upper(coalesce(k.product_name_en, '')) ~ '(^|[^A-Z])YG([^A-Z]|$)' THEN 'YG'
            WHEN upper(coalesce(k.product_name_en, '')) ~ '(^|[^A-Z])PG([^A-Z]|$)' THEN 'PG'
        END
    )
  AND NOT EXISTS (
        SELECT 1 FROM tbt_stock_product sp
        WHERE 'SKU-' || upper(btrim(sp.product_number)) = k.sku_code
    )
  AND NOT EXISTS (
        SELECT 1 FROM stock s
        WHERE NULLIF(btrim(s.codeproduct), '') IS NOT NULL
          AND upper(btrim(s.codeproduct)) = upper(btrim(k.product_number))
    )
  AND k.production_type IS DISTINCT FROM g.name_en;

-- Part 3b: product_type — เดาจากชื่อ (มี EARRING/EARING แต่ยังไม่ใช่ตระกูลต่างหู)
--          ไม่มีต้นทางให้ตรวจ typep จริง จึงเดาจากชื่อสินค้าเป็นข้อมูลสำรอง (เหมือน Part 2 ของไฟล์ 01)
UPDATE tbt_sku k
SET
    product_type = 'E',
    product_type_name = pt.name_th,
    update_date = now(),
    update_by = 'MIGRATION-FIX'
FROM tbm_product_type pt
WHERE pt.code = 'E'
  AND NOT EXISTS (
        SELECT 1 FROM tbt_stock_product sp
        WHERE 'SKU-' || upper(btrim(sp.product_number)) = k.sku_code
    )
  AND NOT EXISTS (
        SELECT 1 FROM stock s
        WHERE NULLIF(btrim(s.codeproduct), '') IS NOT NULL
          AND upper(btrim(s.codeproduct)) = upper(btrim(k.product_number))
    )
  AND (upper(k.product_name_en) LIKE '%EARRING%' OR upper(k.product_name_en) LIKE '%EARING%')
  AND (k.product_type IS NULL OR k.product_type NOT IN ('E', 'ES', 'EL', 'EH'))
  AND k.product_type IS DISTINCT FROM 'E';

-- =============================================
-- Verify (รันแยกหลัง migrate เพื่อตรวจผล)
-- =============================================
-- SELECT product_type, production_type, count(*)
-- FROM tbt_sku
-- GROUP BY product_type, production_type
-- ORDER BY count(*) DESC;
