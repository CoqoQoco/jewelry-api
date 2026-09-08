-- =============================================
-- Migration: Add qty / qty_reserved to tbt_stock_piece
-- Date: 2026-09-08
-- Description: รองรับ silver lot — 1 เลขใหม่เก็บได้หลายชิ้น (qty)
--              available (พร้อมขาย) = qty - qty_reserved คำนวณในโค้ด ไม่เพิ่มคอลัมน์
-- Run order: รันได้ทันที (ปลอดภัย — default ทำให้ IN_STOCK เดิมเท่ากับพฤติกรรมเดิม qty=1 ทุกแถว;
--            แถว SOLD/RESERVED เดิมถูก backfill ด้านล่างให้ตรงสถานะจริง)
--            ต้องรันก่อน deploy API/UI เวอร์ชันที่รู้จัก qty (ดู docs/silver-lot-impact.md §6)
--
-- qty          = จำนวนคงเหลือในเลขนี้ (on hand); ทองได้ 1 อัตโนมัติ (ไม่เคยเปลี่ยน)
-- qty_reserved = จำนวนที่ถูกจองใน SO
-- =============================================

ALTER TABLE tbt_stock_piece
    ADD COLUMN IF NOT EXISTS qty NUMERIC(18,4) NOT NULL DEFAULT 1;

ALTER TABLE tbt_stock_piece
    ADD COLUMN IF NOT EXISTS qty_reserved NUMERIC(18,4) NOT NULL DEFAULT 0;

-- Backfill pieces whose status was already SOLD/RESERVED before this migration ran.
-- The DEFAULT above only fixes newly-untouched rows (qty=1, qty_reserved=0 = old 1-piece
-- behaviour); a SOLD row must not keep qty=1, and a RESERVED row must not keep
-- qty_reserved=0, otherwise the new C# code corrupts stock the first time it touches them:
--   - CancelInvoiceCore does `piece.Qty += N` — a SOLD row stuck at qty=1 becomes qty=2.
--   - ConfirmStockItemsCore / InvoiceService.Create do `piece.QtyReserved -= N` — a RESERVED
--     row stuck at qty_reserved=0 goes negative.
-- Both statements are idempotent (safe to re-run): they only overwrite with the same
-- value they would already have if run before, so running this file twice is a no-op
-- the second time.
UPDATE tbt_stock_piece
    SET qty = 0
    WHERE status = 'SOLD';

UPDATE tbt_stock_piece
    SET qty_reserved = qty
    WHERE status = 'RESERVED';
