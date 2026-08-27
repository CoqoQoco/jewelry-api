-- =============================================
-- Migration: Alter tbt_sale_quotation — Add profit_percent, gold_loss_percent
-- Date: 2026-08-27
-- Description: เพิ่ม column profit_percent + gold_loss_percent ใน quotation
--              รองรับ per-quotation Break Down % (เดิมเป็นค่ากลางทั้งระบบใน Setting)
--              NULL = ให้ FE fallback ไปค่า default ของ Setting เอง ไม่ backfill
-- Re-run safety:
--   - ADD COLUMN IF NOT EXISTS — idempotent
-- =============================================

ALTER TABLE tbt_sale_quotation
    ADD COLUMN IF NOT EXISTS profit_percent NUMERIC;

ALTER TABLE tbt_sale_quotation
    ADD COLUMN IF NOT EXISTS gold_loss_percent NUMERIC;
