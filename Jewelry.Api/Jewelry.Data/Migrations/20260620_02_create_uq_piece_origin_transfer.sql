-- =============================================
-- Migration: Create unique index uq_piece_origin_transfer
-- Date: 2026-06-20
-- Description: safety net ระดับ DB กัน transfer ซ้ำ stock_number_origin
--              ถ้า code fix พลาด การ INSERT piece ซ้ำ origin จะ fail ทันทีด้วย unique violation
--              ถ้าสร้าง index ไม่ขึ้น = ยังมี duplicate เหลือ → กลับไปตรวจ cleanup migration ก่อน
--
-- หมายเหตุ: รันหลัง cleanup (20260620_01) เสร็จและ remaining_dups = 0 เท่านั้น
-- =============================================

CREATE UNIQUE INDEX IF NOT EXISTS uq_piece_origin_transfer
  ON tbt_stock_piece (stock_number_origin)
  WHERE receipt_type='transfer' AND stock_number_origin IS NOT NULL;
