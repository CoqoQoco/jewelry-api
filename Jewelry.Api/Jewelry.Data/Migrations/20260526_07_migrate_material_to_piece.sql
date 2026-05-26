-- =============================================
-- Migration: Migrate tbt_stock_product_material → tbt_stock_piece_material
-- Date: 2026-05-26
-- Description: Phase B.1 — copy legacy material rows into piece-level table
--              join tbt_stock_piece to derive product_code per stock_number
--              ONE-SHOT migration (NOT idempotent) — legacy มี duplicate key
--              legitimate (เช่น piece เดียวมี Sapphire 3 rows แยก) ใช้ NOT EXISTS
--              ไม่ได้เพราะจะ collapse rows
-- Run order: after Phase A scripts (alter_stock_piece_add_product_code,
--            create_tbt_stock_piece_material)
-- Re-run safety: NOT idempotent
--   ถ้าต้อง re-run: TRUNCATE tbt_stock_piece_material RESTART IDENTITY; ก่อน
-- =============================================

INSERT INTO tbt_stock_piece_material (
    stock_number,
    product_code,
    type,
    type_name,
    type_code,
    type_barcode,
    type_origin,
    size,
    qty,
    qty_unit,
    weight,
    weight_unit,
    region,
    price,
    create_date,
    create_by,
    update_date,
    update_by
)
SELECT
    src.stock_number,
    p.product_code,
    src.type,
    src.type_name,
    src.type_code,
    src.type_barcode,
    src.type_origin,
    src.size,
    src.qty,
    src.qty_unit,
    src.weight,
    src.weight_unit,
    src.region,
    src.price,
    src.create_date,
    src.create_by,
    src.update_date,
    src.update_by
FROM tbt_stock_product_material src
JOIN tbt_stock_piece p ON p.stock_number = src.stock_number;

-- Verify (run after migration):
-- SELECT (SELECT COUNT(*) FROM tbt_stock_piece_material)  AS new_count,
--        (SELECT COUNT(*) FROM tbt_stock_product_material) AS legacy_count;
-- ต้องเท่ากัน
