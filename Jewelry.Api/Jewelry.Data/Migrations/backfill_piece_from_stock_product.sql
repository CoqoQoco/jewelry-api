-- =============================================
-- Migration: Backfill Stock Piece from Stock Product (Phase 2 - Stock Refactor Plan D)
-- Date: 2026-05-25
-- Description: (1) Auto-seed locations จาก free-text location ใน tbt_stock_product
--              (2) Insert ข้อมูล per-piece จาก tbt_stock_product เข้า tbt_stock_piece
-- Run order: 2 (ต้องรัน backfill_sku ก่อน เพราะมี FK → tbt_sku)
-- Re-run safe: ทั้ง 2 ส่วนใช้ ON CONFLICT DO NOTHING — idempotent
-- =============================================

-- =============================================
-- Step 1: Auto-seed unknown free-text locations
-- =============================================
-- รวม location ที่ไม่ว่างเปล่า + TRIM + UPPER เป็น code
-- ถ้า location เป็น NULL/blank → piece จะใช้ 'MAIN' (seed แล้วใน Phase 1)
INSERT INTO tbm_stock_location (code, name_th, type, is_active, create_date, create_by)
SELECT DISTINCT
    UPPER(TRIM(location))   AS code,
    location                AS name_th,
    'WAREHOUSE'             AS type,
    TRUE                    AS is_active,
    now()                   AS create_date,
    'BACKFILL'              AS create_by
FROM tbt_stock_product
WHERE NULLIF(TRIM(location), '') IS NOT NULL
ON CONFLICT (code) DO NOTHING;

-- =============================================
-- Step 2: Insert stock pieces
-- =============================================
-- Status rule (legacy 1 row = 1 ชิ้น):
--   qty_sale >= 1 → 'SOLD'   (invoice = ขายขาด; Phase 4 จะแยก RESERVED vs SOLD จาก SO/invoice ถ้าจำเป็น)
--   else          → 'IN_STOCK'
-- location_code: UPPER(TRIM(location)) หรือ 'MAIN' ถ้า NULL/blank
INSERT INTO tbt_stock_piece (
    stock_number,
    sku_code,
    location_code,
    status,
    receipt_number,
    receipt_type,
    receipt_date,
    production_date,
    wo,
    wo_number,
    wo_origin,
    po_number,
    product_cost,
    product_cost_detail,
    weight_actual,
    size_actual,
    barcode,
    remark,
    create_date,
    create_by,
    update_date,
    update_by
)
SELECT
    src.stock_number,
    CASE
        WHEN NULLIF(TRIM(src.product_number), '') IS NOT NULL
            THEN 'SKU-' || UPPER(TRIM(src.product_number))
        ELSE
            'SKU-' || SUBSTRING(
                MD5(
                    LOWER(
                        COALESCE(src.product_name_th, '') ||
                        COALESCE(src.mold, '')             ||
                        COALESCE(src.size, '')             ||
                        COALESCE(src.production_type, '')  ||
                        COALESCE(src.production_type_size, '')
                    )
                ),
                1, 8
            )
    END                                                         AS sku_code,
    COALESCE(NULLIF(UPPER(TRIM(src.location)), ''), 'MAIN')     AS location_code,
    CASE
        WHEN src.qty_sale >= 1 THEN 'SOLD'
        ELSE 'IN_STOCK'
    END                                                         AS status,
    src.receipt_number,
    src.receipt_type,
    src.receipt_date,
    src.production_date,
    src.wo,
    src.wo_number,
    src.wo_origin,
    src.po_number,
    src.product_cost,
    src.product_cost_detail,
    NULL                                                        AS weight_actual,
    NULL                                                        AS size_actual,
    NULL                                                        AS barcode,
    src.remark,
    src.create_date,
    src.create_by,
    src.update_date,
    src.update_by
FROM tbt_stock_product src
ON CONFLICT (stock_number) DO NOTHING;
