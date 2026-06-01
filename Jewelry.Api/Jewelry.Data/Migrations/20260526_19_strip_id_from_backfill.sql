-- =============================================
-- Migration: Drop `id` autoincrement columns from backfill staging
-- Date: 2026-05-26
-- Description: ลบ id จาก staging tables ที่จะ export ไป prod
--              เพราะ prod tbt_stock_piece_material/balance/movement มี PK=id autoincrement
--              ถ้าส่ง id จาก dev ไป prod อาจ collide
-- Run on: jewelry_dev เท่านั้น
-- =============================================

BEGIN;

ALTER TABLE _backfill_piece_material DROP COLUMN IF EXISTS id;
ALTER TABLE _backfill_movement DROP COLUMN IF EXISTS id;

-- tbt_stock_piece, tbt_sku — PK ไม่ใช่ id ก็ลบไป (ถ้ามี)
ALTER TABLE _backfill_piece DROP COLUMN IF EXISTS id;
ALTER TABLE _backfill_sku DROP COLUMN IF EXISTS id;

-- Verify
SELECT
  table_name,
  string_agg(column_name, ', ' ORDER BY ordinal_position) AS cols
FROM information_schema.columns
WHERE table_name IN ('_backfill_sku','_backfill_piece','_backfill_piece_material','_backfill_balance_delta','_backfill_movement')
GROUP BY table_name
ORDER BY table_name;

COMMIT;
