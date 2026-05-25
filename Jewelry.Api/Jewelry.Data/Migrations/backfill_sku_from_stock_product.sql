-- =============================================
-- Migration: Backfill SKU from Stock Product (Phase 2 - Stock Refactor Plan D)
-- Date: 2026-05-25
-- Description: Insert ข้อมูล catalog จาก tbt_stock_product เข้า tbt_sku
--              โดยใช้ natural key dedup ตาม §2.2
-- Run order: 1 (ก่อน backfill_piece, backfill_balance, backfill_movement)
-- Re-run safe: ใช้ ON CONFLICT (sku_code) DO NOTHING — idempotent
-- =============================================

INSERT INTO tbt_sku (
    sku_code,
    product_number,
    product_name_th,
    product_name_en,
    product_type,
    product_type_name,
    mold,
    mold_design,
    stud_earring,
    size,
    production_type,
    production_type_size,
    image_name,
    image_path,
    default_price,
    tag_price_multiplier,
    is_active,
    is_serialized,
    create_date,
    create_by
)
SELECT
    derived.sku_code,
    MIN(src.product_number)         AS product_number,
    MIN(src.product_name_th)        AS product_name_th,
    MIN(src.product_name_en)        AS product_name_en,
    MIN(src.product_type)           AS product_type,
    MIN(src.product_type_name)      AS product_type_name,
    MIN(src.mold)                   AS mold,
    MIN(src.mold_design)            AS mold_design,
    MIN(src.stud_earring)           AS stud_earring,
    MIN(src.size)                   AS size,
    MIN(src.production_type)        AS production_type,
    MIN(src.production_type_size)   AS production_type_size,
    MIN(src.image_name)             AS image_name,
    MIN(src.image_path)             AS image_path,
    MAX(src.product_price)          AS default_price,
    MAX(src.tag_price_multiplier)   AS tag_price_multiplier,
    TRUE                            AS is_active,
    TRUE                            AS is_serialized,
    now()                           AS create_date,
    'BACKFILL'                      AS create_by
FROM tbt_stock_product src
JOIN (
    -- Derive sku_code per row, then group by it
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
) AS derived ON derived.stock_number = src.stock_number
GROUP BY derived.sku_code
ON CONFLICT (sku_code) DO NOTHING;
