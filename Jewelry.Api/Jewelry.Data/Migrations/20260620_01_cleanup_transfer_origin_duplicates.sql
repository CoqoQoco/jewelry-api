-- =============================================
-- Migration: Cleanup duplicate transfer pieces (stock_number_origin)
-- Date: 2026-06-20
-- Description: tbt_stock_piece มี duplicate ของ stock_number_origin จาก receipt_type='transfer'
--              จำนวน 1,589 origin groups (1,590 piece เกิน) เกิดจาก bug ใน dedup seed
--              script นี้เก็บ copy ที่มี business reference (SOLD / sale_order / cost_plan / basket)
--              และลบ duplicate ที่เหลือ (rn > 1) พร้อมปรับ qty_on_hand ใน tbt_stock_balance
--
-- หมายเหตุสำหรับผู้ดูแลระบบ — ตรวจ guard 2 จุดก่อน COMMIT:
--   1. sold_in_delete = 0  (ถ้า > 0 → ROLLBACK ทันที ห้าม COMMIT)
--   2. remaining_dups = 0  (ถ้า > 0 → cleanup ยังไม่สมบูรณ์)
--   3. ตรวจ qty_on_hand ไม่ติดลบ:
--      SELECT * FROM tbt_stock_balance WHERE qty_on_hand < 0;
-- Re-run safety: ใช้ TEMP TABLE — ถ้ารันซ้ำใน transaction เดียวกันจะ error (DROP ก่อนแล้ว ROLLBACK เอง)
-- =============================================

BEGIN;

-- Step 1: ระบุ piece ที่จะลบ (เก็บ rn=1 ต่อ stock_number_origin, ลบ rn>1)
CREATE TEMP TABLE pieces_to_delete AS
SELECT stock_number, product_code, sku_code, location_code, status FROM (
  SELECT p.stock_number, p.product_code, p.sku_code, p.location_code, p.status,
    ROW_NUMBER() OVER (PARTITION BY p.stock_number_origin ORDER BY
      (p.status='SOLD') DESC,
      EXISTS(SELECT 1 FROM tbt_sale_order_product s WHERE s.stock_number=p.stock_number) DESC,
      EXISTS(SELECT 1 FROM tbt_stock_cost_plan c   WHERE c.stock_number=p.stock_number) DESC,
      EXISTS(SELECT 1 FROM tbt_stock_basket_item b WHERE b.stock_number=p.stock_number) DESC,
      p.create_date ASC, p.stock_number ASC) AS rn
  FROM tbt_stock_piece p
  WHERE p.receipt_type='transfer' AND p.stock_number_origin IS NOT NULL
) r WHERE rn > 1;

-- Guard ก่อนลบ: sold_in_delete ต้อง = 0 — ถ้าไม่ใช่ให้ ROLLBACK ทันที
SELECT COUNT(*) AS will_delete, COUNT(*) FILTER (WHERE status='SOLD') AS sold_in_delete FROM pieces_to_delete;

-- Step 2: ลบ child records (ลำดับ FK: child ก่อน parent)
DELETE FROM tbt_stock_piece_material m USING pieces_to_delete d WHERE m.stock_number=d.stock_number AND m.product_code=d.product_code;
DELETE FROM tbt_stock_movement mv USING pieces_to_delete d WHERE mv.stock_number=d.stock_number AND mv.product_code=d.product_code;
DELETE FROM tbt_stock_piece_cost_plan pc USING pieces_to_delete d WHERE pc.stock_number=d.stock_number AND pc.product_code=d.product_code;
DELETE FROM tbt_stock_piece_cost_version pv USING pieces_to_delete d WHERE pv.stock_number=d.stock_number AND pv.product_code=d.product_code;
DELETE FROM tbt_stock_cost_version v USING pieces_to_delete d WHERE v.stock_number=d.stock_number;
DELETE FROM tbt_stock_cost_plan c USING pieces_to_delete d WHERE c.stock_number=d.stock_number;

-- Step 3: ปรับ balance (ลด qty_on_hand เฉพาะ piece ที่ status='IN_STOCK')
WITH adj AS (
  SELECT sku_code, location_code, COUNT(*) AS qty_remove
  FROM pieces_to_delete WHERE status='IN_STOCK'
  GROUP BY sku_code, location_code
)
UPDATE tbt_stock_balance b
SET qty_on_hand = b.qty_on_hand - a.qty_remove, last_movement_at = NOW()
FROM adj a WHERE b.sku_code=a.sku_code AND b.location_code=a.location_code;

-- Step 4: ลบ piece หลัก
DELETE FROM tbt_stock_piece p USING pieces_to_delete d WHERE p.stock_number=d.stock_number AND p.product_code=d.product_code;
DROP TABLE pieces_to_delete;

-- Guard หลังลบ: remaining_dups ต้อง = 0
SELECT COUNT(*) AS remaining_dups FROM (
  SELECT stock_number_origin FROM tbt_stock_piece
  WHERE receipt_type='transfer' AND stock_number_origin IS NOT NULL
  GROUP BY stock_number_origin HAVING COUNT(*)>1) x;

COMMIT;
