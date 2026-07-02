-- Migration: Backfill NULL WO fields on tbt_gold_loss_tang_slip_item from tbt_production_plan
-- Date: 2026-07-02
-- Description: แก้ไขข้อมูลเก่าที่ wo/wo_number/product_number/product_name/gold_size เป็น NULL
--              เนื่องจาก FE ส่งแค่ production_plan_id + item_no — join plan เพื่อเติมค่า
UPDATE tbt_gold_loss_tang_slip_item i
SET
    wo            = p.wo,
    wo_number     = p.wo_number,
    product_number = p.product_number,
    product_name  = p.product_name,
    gold_size     = p.type_size
FROM tbt_production_plan p
WHERE i.production_plan_id = p.id
  AND (
      i.wo IS NULL
      OR i.wo_number IS NULL
      OR i.product_number IS NULL
      OR i.product_name IS NULL
      OR i.gold_size IS NULL
  );
