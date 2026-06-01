-- =============================================
-- Migration: Rebuild TRANSFER_18K movements for kept pieces
-- Date: 2026-05-26
-- Description: หลัง cleanup 16, 18K pieces 3,867 ตัวแต่ movements เหลือ 2,000 (1,867 orphan)
--              สาเหตุ: stock_number ถูกใช้ข้าม batch — cleanup เผลอลบ movement ที่ผูก piece ที่เก็บไว้
--              แก้: ลบ TRANSFER_18K movements เก่าวันนี้ทั้งหมด → recreate 1 RECEIPT ต่อ piece
-- Re-run safety: idempotent ผ่าน DELETE + INSERT pattern (ทำใหม่ได้)
-- =============================================

BEGIN;

-- Step 1: ลบ TRANSFER_18K movements วันนี้ทั้งหมด
DELETE FROM tbt_stock_movement
WHERE ref_doc_type = 'TRANSFER_18K'
  AND create_date >= '2026-05-26'::date;

-- Step 2: recreate 1 RECEIPT movement ต่อ piece (จาก piece data)
INSERT INTO tbt_stock_movement (
  movement_date, movement_type, sku_code, stock_number, product_code,
  from_location, to_location, qty, ref_doc_type, ref_doc_no, remark,
  create_date, create_by
)
SELECT
  p.receipt_date AS movement_date,
  'RECEIPT' AS movement_type,
  p.sku_code,
  p.stock_number,
  p.product_code,
  NULL AS from_location,
  p.location_code AS to_location,
  1 AS qty,
  'TRANSFER_18K' AS ref_doc_type,
  p.receipt_number AS ref_doc_no,
  NULL AS remark,
  p.create_date,
  COALESCE(p.create_by, 'system') AS create_by
FROM tbt_stock_piece p
WHERE p.stock_number LIKE 'DK-18K%'
  AND p.create_date >= '2026-05-26'::date;

-- Verify
SELECT
  (SELECT COUNT(*) FROM tbt_stock_piece WHERE stock_number LIKE 'DK-18K%' AND create_date >= '2026-05-26'::date) AS pieces,
  (SELECT COUNT(*) FROM tbt_stock_movement WHERE ref_doc_type='TRANSFER_18K' AND create_date >= '2026-05-26'::date) AS movements;
-- expect: 3867, 3867

COMMIT;
