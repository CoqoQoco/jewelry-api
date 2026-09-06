-- =============================================
-- Migration: Replace uq_piece_origin_transfer with a non-unique index
-- Date: 2026-09-06
-- Description: ยกเลิกข้อบังคับ "1 รหัสเดิม = 1 piece" เพื่อรองรับงานเงิน
-- Run order: ก่อนรัน Transfer/SILVER รอบแรก (batch 5-9-2026)
--
-- ทำไมต้องยกเลิก
--   uq_piece_origin_transfer (สร้างไว้ที่ 20260620_02) เป็น safety net กัน transfer ซ้ำ
--   โดยตั้งสมมติฐานว่า 1 รหัสสินค้าเดิม (stock_number_origin) = 1 ชิ้นเสมอ
--   สมมติฐานนี้จริงกับงานทอง แต่ไม่จริงกับงานเงิน — ลูกค้าส่ง quantity มาต่อรหัส
--   batch 5-9-2026: 2,345 รหัส = 12,940 ชิ้น โดย 1,894 รหัส (12,489 ชิ้น) มี qty > 1
--   ถ้าคง unique index ไว้ INSERT จะ fail ด้วย unique violation ตั้งแต่รอบแรก
--
-- ทำไมถึงยังปลอดภัยหลังยกเลิก — ตัวกันโอนซ้ำที่ทำงานจริงยังอยู่ครบ 2 ชั้น
--   1) OldStockService.TransferStockCore เช็ค existingPieceProductCodes
--      (query piece ที่ receipt_type='transfer' และ stock_number_origin ตรงกัน) ก่อน insert ทุกครั้ง
--   2) คิว (stock_9k / stock_18k / stock_silver) มี is_transfer flag ที่ commit
--      ในทรานแซกชันเดียวกับการสร้าง piece → re-run ไม่หยิบแถวเดิมซ้ำ
--   index นี้เป็นชั้นที่ 3 (belt-and-braces) ซึ่งตอนนี้ขัดกับ domain จริงของงานเงิน
--
-- ถ้าต้องการย้อนกลับ (ทำได้เฉพาะตอนที่ยังไม่มีข้อมูลเงินหรือรหัสซ้ำ)
--   DROP INDEX IF EXISTS ix_piece_origin_transfer;
--   CREATE UNIQUE INDEX uq_piece_origin_transfer ON tbt_stock_piece (stock_number_origin)
--     WHERE receipt_type='transfer' AND stock_number_origin IS NOT NULL;
-- =============================================

DROP INDEX IF EXISTS uq_piece_origin_transfer;

-- คงเป็น index ธรรมดา — query ที่พึ่ง index นี้ (dedupe check ใน TransferStockCore) ยังเร็วเท่าเดิม
CREATE INDEX IF NOT EXISTS ix_piece_origin_transfer
  ON tbt_stock_piece (stock_number_origin)
  WHERE receipt_type = 'transfer' AND stock_number_origin IS NOT NULL;
