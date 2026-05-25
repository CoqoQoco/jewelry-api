-- =============================================
-- Migration: Backfill Movement History from Pieces (Phase 2 - Stock Refactor Plan D)
-- Date: 2026-05-25
-- Description: Seed historical RECEIPT + SALE movements จาก tbt_stock_piece
--              - 1 RECEIPT row ต่อ piece ที่มี receipt_date
--              - 1 SALE row ต่อ piece ที่ status = 'SOLD'
-- Run order: 4 (ต้องรัน backfill_piece ก่อน)
-- Re-run safe: ตาราง tbt_stock_movement ไม่มี natural key → ไม่สามารถใช้ ON CONFLICT
--              *** ONE-SHOT SCRIPT — ห้ามรันซ้ำถ้า movement rows มีอยู่แล้ว ***
--              ก่อนรันซ้ำให้ตรวจก่อนว่า create_by = 'BACKFILL' มีอยู่กี่ row:
--                SELECT COUNT(*) FROM tbt_stock_movement WHERE create_by = 'BACKFILL';
--              ถ้าต้องการ re-seed ทั้งหมด: DELETE FROM tbt_stock_movement WHERE create_by = 'BACKFILL';
-- =============================================

-- =============================================
-- Step 1: RECEIPT history
-- =============================================
-- 1 row ต่อ piece ที่มี receipt_date ไม่ว่างเปล่า
INSERT INTO tbt_stock_movement (
    movement_date,
    movement_type,
    sku_code,
    stock_number,
    from_location,
    to_location,
    qty,
    ref_doc_type,
    ref_doc_no,
    remark,
    create_date,
    create_by
)
SELECT
    receipt_date        AS movement_date,
    'RECEIPT'           AS movement_type,
    sku_code,
    stock_number,
    NULL                AS from_location,
    location_code       AS to_location,
    1                   AS qty,
    'RECEIPT'           AS ref_doc_type,
    receipt_number      AS ref_doc_no,
    NULL                AS remark,
    now()               AS create_date,
    'BACKFILL'          AS create_by
FROM tbt_stock_piece
WHERE receipt_date IS NOT NULL;

-- =============================================
-- Step 2: SALE history (สำหรับ piece ที่ SOLD แล้ว)
-- =============================================
-- ใช้ update_date เป็น movement_date (วันที่ขาย) หรือ create_date ถ้า update_date ว่างเปล่า
-- ref_doc_no = stock_number (legacy ไม่มีเลข invoice ใน piece — Phase 4 อาจ enrich จาก SO/invoice)
INSERT INTO tbt_stock_movement (
    movement_date,
    movement_type,
    sku_code,
    stock_number,
    from_location,
    to_location,
    qty,
    ref_doc_type,
    ref_doc_no,
    remark,
    create_date,
    create_by
)
SELECT
    COALESCE(update_date, create_date)  AS movement_date,
    'SALE'                              AS movement_type,
    sku_code,
    stock_number,
    location_code                       AS from_location,
    NULL                                AS to_location,
    1                                   AS qty,
    'LEGACY_SALE'                       AS ref_doc_type,
    stock_number                        AS ref_doc_no,
    NULL                                AS remark,
    now()                               AS create_date,
    'BACKFILL'                          AS create_by
FROM tbt_stock_piece
WHERE status = 'SOLD';
