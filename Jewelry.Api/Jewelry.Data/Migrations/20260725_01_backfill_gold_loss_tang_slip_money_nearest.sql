-- =============================================
-- Migration: Fix Gold Loss Tang slip money rounding (ceiling -> round-half-up)
-- Date: 2026-07-25
-- Description:
--   total_money_diff เดิมคำนวณด้วย ceiling (MidpointRounding.ToPositiveInfinity)
--   แก้ business rule เป็น ปัดปกติ half-up ให้ตรงกับ preview บนหน้าจอ (frontend Math.round)
--   allowed_loss / diff_loss ไม่ต้องแก้ (เป็น 4 ตำแหน่งพอดีอยู่แล้ว ค่าเท่ากันทั้งสองวิธี)
--   สูตร half-up: FLOOR(diff_loss*100 + 0.5)/100 * price_per_gram  (ตรงกับ JS Math.round)
--   กระทบเฉพาะใบที่ค่าเปลี่ยนจริง (active เท่านั้น)
-- =============================================
UPDATE tbt_gold_loss_tang_slip
SET total_money_diff = FLOOR(diff_loss * 100 + 0.5) / 100 * price_per_gram,
    update_date = NOW(),
    update_by = 'SYSTEM-ROUNDING-FIX'
WHERE is_active = true
  AND total_money_diff <> FLOOR(diff_loss * 100 + 0.5) / 100 * price_per_gram;
