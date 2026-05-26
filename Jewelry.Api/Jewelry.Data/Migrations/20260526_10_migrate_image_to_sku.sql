-- =============================================
-- Migration: Migrate tbt_stock_product_image → tbt_sku_image
-- Date: 2026-05-26
-- Description: Phase B.4 — promote product images to SKU-level catalog
--              join path:
--                tbt_stock_product_image (img) — shared image pool (keyed by name+year)
--                ← tbt_stock_product.image_name = img.name
--                → tbt_stock_piece.stock_number = tbt_stock_product.stock_number
--                → tbt_sku.sku_code = tbt_stock_piece.sku_code
--              DISTINCT on (sku_code, img.name) to dedup:
--              multiple pieces of the same SKU share the same image name
--              fields: sku_code, name, name_path, year, is_active, remark, audit cols
--              sort_order: tbt_stock_product_image has no sort_order — default to NULL
-- Run order: after Phase A scripts (create_tbt_sku_image, backfill_piece_from_stock_product)
-- Re-run safety: NOT EXISTS guard on (sku_code, name) — idempotent
-- =============================================

INSERT INTO tbt_sku_image (
    sku_code,
    name,
    name_path,
    year,
    sort_order,
    is_active,
    remark,
    create_date,
    create_by,
    update_date,
    update_by
)
SELECT DISTINCT ON (p.sku_code, img.name)
    p.sku_code,
    img.name,
    img.name_path,
    img.year,
    NULL::INT          AS sort_order,
    img.is_active,
    img.remark,
    img.create_date,
    img.create_by,
    img.update_date,
    img.update_by
FROM tbt_stock_product_image img
JOIN tbt_stock_product sp  ON sp.image_name    = img.name
JOIN tbt_stock_piece   p   ON p.stock_number   = sp.stock_number
JOIN tbt_sku           s   ON s.sku_code       = p.sku_code
WHERE NOT EXISTS (
    SELECT 1
    FROM tbt_sku_image si
    WHERE si.sku_code = p.sku_code
      AND si.name     = img.name
);

-- Verify (run after migration):
-- SELECT (SELECT COUNT(*) FROM tbt_sku_image)             AS new_count,
--        (SELECT COUNT(*) FROM tbt_stock_product_image)   AS legacy_pool_count,
--        (SELECT COUNT(DISTINCT sp.image_name || '|' || p.sku_code)
--           FROM tbt_stock_product sp
--           JOIN tbt_stock_piece p ON p.stock_number = sp.stock_number
--          WHERE sp.image_name IS NOT NULL)               AS expected_pairs;
