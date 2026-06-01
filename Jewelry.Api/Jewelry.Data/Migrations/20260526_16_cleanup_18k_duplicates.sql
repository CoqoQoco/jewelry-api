-- =============================================
-- Migration: Cleanup duplicate 18K pieces from import batch 25-5-2026
-- Date: 2026-05-26
-- Description: Transfer/18K endpoint ถูกเรียกซ้ำ ทำให้สร้าง piece เกิน 3,000 ตัว
--              เก็บ piece เก่าสุด (rn=1) ต่อ stock_number_origin, ลบ rn>1 (3,000 รายการ)
--              + ลบ piece_material + movement + adjust balance
-- Re-run safety: ใช้ TEMP TABLE — รันได้ครั้งเดียว
-- =============================================

BEGIN;

-- Step 1: identify duplicates
CREATE TEMP TABLE pieces_to_delete AS
SELECT stock_number, product_code, sku_code, location_code
FROM (
  SELECT
    stock_number, product_code, sku_code, location_code,
    stock_number_origin, create_date,
    ROW_NUMBER() OVER (PARTITION BY stock_number_origin ORDER BY create_date ASC, stock_number ASC) AS rn
  FROM tbt_stock_piece
  WHERE stock_number LIKE 'DK-18K%' AND create_date >= '2026-05-26'::date
) ranked
WHERE rn > 1;

SELECT COUNT(*) AS will_delete FROM pieces_to_delete;
-- expect: 3000

-- Step 2: delete piece materials
DELETE FROM tbt_stock_piece_material
WHERE (stock_number, product_code) IN (
  SELECT stock_number, product_code FROM pieces_to_delete
);

-- Step 3: delete movements (RECEIPT จาก batch นี้)
DELETE FROM tbt_stock_movement
WHERE stock_number IN (SELECT stock_number FROM pieces_to_delete)
  AND ref_doc_type = 'TRANSFER_18K'
  AND create_date >= '2026-05-26'::date;

-- Step 4: adjust balance (qty_on_hand ลดตามจำนวน piece ที่ลบ ต่อ sku+location)
WITH adjustment AS (
  SELECT sku_code, location_code, COUNT(*) AS qty_to_remove
  FROM pieces_to_delete
  GROUP BY sku_code, location_code
)
UPDATE tbt_stock_balance b
SET qty_on_hand = b.qty_on_hand - a.qty_to_remove,
    last_movement_at = NOW()
FROM adjustment a
WHERE b.sku_code = a.sku_code AND b.location_code = a.location_code;

-- Step 5: delete pieces
DELETE FROM tbt_stock_piece
WHERE (stock_number, product_code) IN (
  SELECT stock_number, product_code FROM pieces_to_delete
);

DROP TABLE pieces_to_delete;

-- Verify counts
SELECT
  (SELECT COUNT(*) FROM tbt_stock_piece WHERE stock_number LIKE 'DK-18K%' AND create_date >= '2026-05-26'::date) AS pieces_left,
  (SELECT COUNT(*) FROM tbt_stock_movement WHERE ref_doc_type='TRANSFER_18K' AND create_date >= '2026-05-26'::date) AS movements_left;
-- expect: 3867, 3867

COMMIT;
