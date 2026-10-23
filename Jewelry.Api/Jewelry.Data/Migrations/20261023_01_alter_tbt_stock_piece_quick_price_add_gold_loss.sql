-- =============================================
-- Migration: Quick Price เพิ่มราคา "ทุนทอง + Alloy (รวม Gold loss)"
-- Date: 2026-10-23
-- Description: เพิ่มคอลัมน์ Gold loss + ราคาบรรทัดที่ 3 ใน tbt_stock_piece_quick_price
--   ของเดิม (quick_price / gold_cost_total / total_price) คงไว้ไม่เปลี่ยน
--   สูตรเพิ่ม (ไม่ปัดระหว่างทาง ปัดเฉพาะราคาสุดท้าย) — ตรงกับใบเสนอราคา (breakdown):
--   alloy_weight            = gold_weight * (1 + gold_loss_percent/100) * alloy_factor   (UI ตั้งต้น แก้ได้)
--   gold_loss_cost_total    = gold_weight * (1 + gold_loss_percent/100) * gold_price
--                             + alloy_weight * alloy_price                               (alloy ไม่บวก gold loss ซ้ำ)
--   gold_loss_total_price   = ปัด(cost_ex_gold * (1 + percent/100) + gold_loss_cost_total)
--   gold_loss_percent ตั้งต้นจาก setting breakdown (goldLossPercent, ค่าเริ่ม 12)
--   ทุกคอลัมน์ NULLABLE — แถวเดิมก่อนหน้านี้ค่าเป็น NULL
-- Re-run safety: Idempotent — ADD COLUMN IF NOT EXISTS
-- ⚠️ รันบน prod ก่อน deploy API
-- =============================================

ALTER TABLE tbt_stock_piece_quick_price
    ADD COLUMN IF NOT EXISTS gold_loss_percent     NUMERIC,
    ADD COLUMN IF NOT EXISTS gold_loss_cost_total  NUMERIC,
    ADD COLUMN IF NOT EXISTS gold_loss_total_price NUMERIC;
