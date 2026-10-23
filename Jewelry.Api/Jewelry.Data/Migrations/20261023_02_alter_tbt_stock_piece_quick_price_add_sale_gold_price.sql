-- =============================================
-- Migration: Quick Price แยกราคาทอง "ตอนผลิต" กับ "วันขาย"
-- Date: 2026-10-23
-- Ticket: TK202610230009
-- Description: เพิ่มคอลัมน์ราคาทอง ณ วันขาย ใน tbt_stock_piece_quick_price
--   gold_price (เดิม)  = ราคาทองตอนผลิต ใช้คำนวณ cost_ex_gold (ไม่เปลี่ยน)
--   sale_gold_price    = ราคาทอง/กรัม ณ วันขาย ใช้คำนวณ total_price / gold_loss_cost_total
--   sale_gold_cost_total = gold_weight * sale_gold_price + alloy_weight * alloy_price
--   ทั้งสองคอลัมน์ NULLABLE — NULL = ใบเก่า ให้ใช้ gold_price / gold_cost_total แทน
-- Re-run safety: Idempotent — ADD COLUMN IF NOT EXISTS
-- ⚠️ รันบน prod ก่อน deploy API
-- =============================================

ALTER TABLE tbt_stock_piece_quick_price
    ADD COLUMN IF NOT EXISTS sale_gold_price      NUMERIC,   -- ราคาทอง/กรัม ณ วันขาย (NULL = ใบเก่า ใช้ gold_price)
    ADD COLUMN IF NOT EXISTS sale_gold_cost_total NUMERIC;   -- นน.ทอง × sale_gold_price + ค่า Alloy

-- =============================================
-- Rollback (รันเมื่อจำเป็นเท่านั้น)
-- =============================================
-- ALTER TABLE tbt_stock_piece_quick_price
--     DROP COLUMN IF EXISTS sale_gold_price,
--     DROP COLUMN IF EXISTS sale_gold_cost_total;
