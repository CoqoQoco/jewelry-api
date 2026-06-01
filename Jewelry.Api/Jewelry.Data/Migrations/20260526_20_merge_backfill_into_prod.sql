-- =============================================
-- Migration: Merge backfill staging tables into prod real tables
-- Date: 2026-05-26
-- Description: หลัง export _backfill_* จาก dev → prod staging แล้ว
--              รัน INSERT ... ON CONFLICT DO NOTHING เพื่อ merge เข้า real tables
-- Run on: PROD เท่านั้น (มี _backfill_* แล้ว 5 ตาราง)
-- Re-run safety: ON CONFLICT — idempotent
-- =============================================

BEGIN;

-- 1. SKU (PK = sku_code)
INSERT INTO tbt_sku (
  sku_code, product_number, product_name_th, product_name_en, product_type, product_type_name,
  mold, mold_design, stud_earring, size, production_type, production_type_size,
  image_name, image_path, default_price, tag_price_multiplier, is_active, is_serialized,
  remark, create_date, create_by, update_date, update_by
)
SELECT
  sku_code, product_number, product_name_th, product_name_en, product_type, product_type_name,
  mold, mold_design, stud_earring, size, production_type, production_type_size,
  image_name, image_path, default_price, tag_price_multiplier, is_active, is_serialized,
  remark, create_date, create_by, update_date, update_by
FROM _backfill_sku
ON CONFLICT (sku_code) DO NOTHING;

-- 2. Stock pieces (PK = stock_number, product_code)
INSERT INTO tbt_stock_piece (
  stock_number, sku_code, location_code, status, receipt_number, receipt_type,
  receipt_date, production_date, wo, wo_number, wo_origin, po_number,
  product_cost, product_cost_detail, weight_actual, size_actual, barcode, remark,
  create_date, create_by, update_date, update_by, product_code, stock_number_origin
)
SELECT
  stock_number, sku_code, location_code, status, receipt_number, receipt_type,
  receipt_date, production_date, wo, wo_number, wo_origin, po_number,
  product_cost, product_cost_detail, weight_actual, size_actual, barcode, remark,
  create_date, create_by, update_date, update_by, product_code, stock_number_origin
FROM _backfill_piece
ON CONFLICT (stock_number, product_code) DO NOTHING;

-- 3. Piece materials (PK = id autoincrement) — append-only
INSERT INTO tbt_stock_piece_material (
  stock_number, product_code, type, type_name, type_code, type_barcode, type_origin,
  size, qty, qty_unit, weight, weight_unit, region, price,
  create_date, create_by, update_date, update_by
)
SELECT
  stock_number, product_code, type, type_name, type_code, type_barcode, type_origin,
  size, qty, qty_unit, weight, weight_unit, region, price,
  create_date, create_by, update_date, update_by
FROM _backfill_piece_material;

-- 4. Stock movements (PK = id autoincrement) — append-only
INSERT INTO tbt_stock_movement (
  movement_date, movement_type, sku_code, stock_number, product_code,
  from_location, to_location, qty, ref_doc_type, ref_doc_no, remark,
  create_date, create_by
)
SELECT
  movement_date, movement_type, sku_code, stock_number, product_code,
  from_location, to_location, qty, ref_doc_type, ref_doc_no, remark,
  create_date, create_by
FROM _backfill_movement;

-- 5. Balance UPSERT (add qty_on_hand per sku+location based on new pieces)
INSERT INTO tbt_stock_balance (sku_code, location_code, qty_on_hand, qty_reserved, last_movement_at, create_by, create_date)
SELECT
  p.sku_code,
  p.location_code,
  COUNT(*) AS qty_on_hand,
  0 AS qty_reserved,
  NOW() AS last_movement_at,
  'BACKFILL' AS create_by,
  NOW() AS create_date
FROM _backfill_piece p
GROUP BY p.sku_code, p.location_code
ON CONFLICT (sku_code, location_code)
DO UPDATE SET
  qty_on_hand = tbt_stock_balance.qty_on_hand + EXCLUDED.qty_on_hand,
  last_movement_at = NOW();

-- Verify
SELECT
  (SELECT COUNT(*) FROM tbt_sku) AS prod_sku_total,
  (SELECT COUNT(*) FROM tbt_stock_piece WHERE create_date >= '2026-05-26'::date AND receipt_type='transfer') AS new_pieces,
  (SELECT COUNT(*) FROM tbt_stock_movement WHERE ref_doc_type IN ('TRANSFER_9K','TRANSFER_14K','TRANSFER_18K')) AS new_movements,
  (SELECT SUM(qty_on_hand)::bigint FROM tbt_stock_balance) AS total_balance;

COMMIT;

-- ลบ staging ทิ้งหลัง verify ok (รันแยก):
-- DROP TABLE _backfill_sku, _backfill_piece, _backfill_piece_material, _backfill_movement;
