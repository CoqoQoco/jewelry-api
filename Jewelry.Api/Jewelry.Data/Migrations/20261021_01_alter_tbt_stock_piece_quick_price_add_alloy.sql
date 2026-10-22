-- =============================================
-- Migration: Quick Price เพิ่ม Alloy (TK202610210002)
-- Date: 2026-10-21
-- Description: เพิ่มคอลัมน์ Alloy + ราคาขายรวมทอง ใน tbt_stock_piece_quick_price
--   สูตรใหม่ (ไม่ปัดระหว่างทาง ปัดเฉพาะราคาสุดท้าย):
--   gold_cost_total = gold_weight * gold_price + alloy_weight * alloy_price   (ทุนทอง = ค่าทอง + ค่า alloy)
--   cost_ex_gold    = cost - gold_cost_total                                  (ทุนเพชรพลอย+ค่าแรง)
--   quick_price     = ปัด(cost_ex_gold * (1 + percent/100))                   (ราคาไม่รวมทอง)
--   total_price     = ปัด(cost_ex_gold * (1 + percent/100) + gold_cost_total) (ราคาขายใหม่รวมทุนทอง)
--   alloy_weight = น้ำหนัก alloy ที่ใช้คิดจริง (UI ตั้งต้น gold_weight * 1.12 * alloy_factor แก้ได้)
--   karat เช่น '18K','14K','9K' หรือ NULL = ไม่คิด alloy
--   ทุกคอลัมน์ NULLABLE — แถวเดิมก่อนหน้านี้ค่าเป็น NULL
-- Re-run safety: Idempotent — ADD COLUMN IF NOT EXISTS
-- =============================================

ALTER TABLE tbt_stock_piece_quick_price
    ADD COLUMN IF NOT EXISTS karat           CHARACTER VARYING,
    ADD COLUMN IF NOT EXISTS alloy_factor    NUMERIC,
    ADD COLUMN IF NOT EXISTS alloy_weight    NUMERIC,
    ADD COLUMN IF NOT EXISTS alloy_price     NUMERIC,
    ADD COLUMN IF NOT EXISTS gold_cost_total NUMERIC,
    ADD COLUMN IF NOT EXISTS total_price     NUMERIC;
