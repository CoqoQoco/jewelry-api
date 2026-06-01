-- =============================================
-- Migration: Create staging tables for PROD backfill
-- Date: 2026-05-26
-- Description: รวบรวมข้อมูลใหม่ vันนี้ (5,200 pieces จาก batch 25-5-2026)
--              เป็น 5 staging tables → User export ไป prod ผ่าน DBeaver
-- Run on: jewelry_dev เท่านั้น
-- =============================================

BEGIN;

-- ใช้ ref_doc_no (=receipt_number) เป็น batch identifier — กรอง batch นี้
-- Receipt prefixes: DK9K..., DK14K..., DK18K... ที่สร้างวันนี้

-- ลบ staging เก่าถ้ามี
DROP TABLE IF EXISTS _backfill_sku;
DROP TABLE IF EXISTS _backfill_piece;
DROP TABLE IF EXISTS _backfill_piece_material;
DROP TABLE IF EXISTS _backfill_balance_delta;
DROP TABLE IF EXISTS _backfill_movement;

-- =============================================
-- 1. SKU ใหม่ (created today) — ต้อง INSERT ลง prod
-- =============================================
CREATE TABLE _backfill_sku AS
SELECT *
FROM tbt_sku
WHERE create_date >= '2026-05-26'::date;

-- =============================================
-- 2. Pieces ใหม่ (5,200)
-- =============================================
CREATE TABLE _backfill_piece AS
SELECT *
FROM tbt_stock_piece
WHERE create_date >= '2026-05-26'::date AND receipt_type = 'transfer';

-- =============================================
-- 3. Piece materials ของ piece ใหม่
-- =============================================
CREATE TABLE _backfill_piece_material AS
SELECT pm.*
FROM tbt_stock_piece_material pm
JOIN _backfill_piece p ON p.stock_number = pm.stock_number AND p.product_code = pm.product_code;

-- =============================================
-- 4. Balance delta (sku, location, qty เพิ่ม)
-- =============================================
CREATE TABLE _backfill_balance_delta AS
SELECT
  sku_code,
  location_code,
  COUNT(*) AS qty_delta
FROM _backfill_piece
GROUP BY sku_code, location_code;

-- =============================================
-- 5. Movements ใหม่ (TRANSFER_*)
-- =============================================
CREATE TABLE _backfill_movement AS
SELECT *
FROM tbt_stock_movement
WHERE create_date >= '2026-05-26'::date
  AND ref_doc_type IN ('TRANSFER_9K', 'TRANSFER_14K', 'TRANSFER_18K');

-- Verify counts
SELECT
  (SELECT COUNT(*) FROM _backfill_sku) AS new_sku,
  (SELECT COUNT(*) FROM _backfill_piece) AS new_piece,
  (SELECT COUNT(*) FROM _backfill_piece_material) AS new_material,
  (SELECT COUNT(*) FROM _backfill_balance_delta) AS balance_rows,
  (SELECT COUNT(*) FROM _backfill_movement) AS new_movement;

COMMIT;
