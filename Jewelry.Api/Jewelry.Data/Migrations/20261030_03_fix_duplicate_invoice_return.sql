-- =============================================
-- Migration: Fix duplicate RETURN (INVOICE_DELETE) stock movements
-- Date: 2026-10-30
-- Ticket: TK202610290006
-- Description: การลบ invoice ถูกยิงซ้ำ (double-submit) ทำให้ ledger มีแถว RETURN / INVOICE_DELETE ซ้ำ
--              (ห่างกันไม่กี่วินาที) — ยอด piece/balance ถูกต้องอยู่แล้ว มีแต่แถว ledger เกิน
--              ลบ "แถวหลัง" ของแต่ละคู่ที่ซ้ำ (stock_number + product_code + ref_doc_no เดียวกัน, ห่างกัน < 5 วินาที)
--              โดยสำรองแถวที่ลบไว้ใน tbt_stock_movement_dup_return_bak ก่อน
-- Rollback: 20261030_03_fix_duplicate_invoice_return_rollback.sql
-- ไม่มีส่วนใดของไฟล์นี้แตะ DK-18K-1XR-4293 (รอร้านยืนยัน — ดู DRAFT ท้ายไฟล์)
-- =============================================

BEGIN;

-- =============================================
-- 1. Backup tables (โครงสร้างเดียวกับตารางต้นทาง)
-- =============================================
CREATE TABLE IF NOT EXISTS tbt_stock_movement_dup_return_bak AS
SELECT * FROM tbt_stock_movement WHERE false;

CREATE TABLE IF NOT EXISTS tbt_stock_movement_unit_dup_return_bak AS
SELECT * FROM tbt_stock_movement_unit WHERE false;

-- =============================================
-- 2. หาแถวซ้ำ (เก็บแถวแรก ลบแถวหลัง) แล้วสำรอง
-- =============================================
CREATE TEMP TABLE tmp_dup_return_ids ON COMMIT DROP AS
SELECT id
FROM (
    SELECT m.id,
           m.movement_date,
           LAG(m.movement_date) OVER (
               PARTITION BY m.stock_number, m.product_code, m.ref_doc_no
               ORDER BY m.movement_date, m.id
           ) AS prev_date
    FROM tbt_stock_movement m
    WHERE m.movement_type = 'RETURN'
      AND m.ref_doc_type = 'INVOICE_DELETE'
) x
WHERE x.prev_date IS NOT NULL
  AND x.movement_date - x.prev_date < INTERVAL '5 seconds';

INSERT INTO tbt_stock_movement_dup_return_bak
SELECT m.*
FROM tbt_stock_movement m
JOIN tmp_dup_return_ids d ON d.id = m.id
WHERE NOT EXISTS (SELECT 1 FROM tbt_stock_movement_dup_return_bak b WHERE b.id = m.id);

INSERT INTO tbt_stock_movement_unit_dup_return_bak
SELECT mu.*
FROM tbt_stock_movement_unit mu
JOIN tmp_dup_return_ids d ON d.id = mu.movement_id
WHERE NOT EXISTS (SELECT 1 FROM tbt_stock_movement_unit_dup_return_bak b WHERE b.id = mu.id);

-- =============================================
-- 3. ลบ (ลูกก่อนแม่)
-- =============================================
DELETE FROM tbt_stock_movement_unit
WHERE movement_id IN (SELECT id FROM tmp_dup_return_ids);

DELETE FROM tbt_stock_movement
WHERE id IN (SELECT id FROM tmp_dup_return_ids);

-- ตรวจก่อน COMMIT: จำนวนที่สำรองต้องเท่ากับที่คาดไว้ (10 แถว) — ถ้าไม่ใช่ให้ ROLLBACK
SELECT COUNT(*) AS backed_up_rows FROM tbt_stock_movement_dup_return_bak;

COMMIT;

-- =============================================
-- Verification (รันหลัง COMMIT)
-- =============================================
-- 1) แถวที่ถูกลบ ต้องได้ 10 แถว (10 lot ในใบ INV260907007 ฯลฯ)
-- SELECT id, stock_number, product_code, ref_doc_no, movement_date FROM tbt_stock_movement_dup_return_bak ORDER BY stock_number, movement_date;
-- SELECT COUNT(*) FROM tbt_stock_movement_dup_return_bak;  -- expected 10

-- 2) ต้องไม่เหลือ RETURN / INVOICE_DELETE ซ้ำต่อ (stock_number, product_code, ref_doc_no) ภายใน 5 วินาที — expected 0 แถว
-- SELECT stock_number, product_code, ref_doc_no, COUNT(*)
-- FROM tbt_stock_movement
-- WHERE movement_type = 'RETURN' AND ref_doc_type = 'INVOICE_DELETE'
-- GROUP BY stock_number, product_code, ref_doc_no
-- HAVING COUNT(*) > 1;

-- 3) ล็อตตัวอย่างต้องเหลือ RETURN 1 แถวต่อใบ
-- SELECT stock_number, movement_type, ref_doc_type, ref_doc_no, movement_date
-- FROM tbt_stock_movement
-- WHERE stock_number = 'DK-18K-20H-239' ORDER BY movement_date;

-- =============================================
-- DRAFT — DK-18K-1XR-4293 (ห้ามรัน! รอร้านยืนยันก่อน ไม่มี statement ที่รันได้ในส่วนนี้)
-- =============================================
-- ที่มา: ชิ้นเดียวถูกขายสองครั้ง (INV260829005 และ INV260829006) แล้วลบ INV260829005 ทำให้ RETURN คืนชิ้นกลับ
--        ปัจจุบัน: piece qty 1, qty_reserved 1, status RESERVED; balance STOCK01-2 on_hand 0 reserved 1
-- ใบที่ถือครองจริงคือ INV260829006 (SO260829003) จึงควรแก้ให้เป็นขายแล้วภายใต้ใบนี้:
--   1) tbt_stock_piece (stock_number = 'DK-18K-1XR-4293'): qty 0, qty_reserved 0, status 'SOLD'
--   2) tbt_stock_unit ของล็อตนี้: status 'SOLD', sold_ref 'INV260829006', sold_date = เวลาออกใบ, ล้าง reserved_ref_type/reserved_ref/reserved_line_id
--   3) tbt_stock_balance (SKU ของชิ้นนี้, location STOCK01-2): qty_reserved 1 -> 0 (on_hand คงที่ 0)
--   4) SO260829002 (บรรทัดที่ invoice เป็น null และยังจองชิ้นเดียวกัน): TBD — ต้องถามร้านว่ายกเลิกบรรทัด/ปลด RESERVE หรือเติมของใหม่แทน
--   5) ledger: พิจารณาเพิ่ม movement ปรับ (ADJUST) หรือเก็บ RETURN ของ INV260829005 ไว้เป็นประวัติ — TBD ตามที่ร้านตัดสินใจ
-- เมื่อร้านยืนยันแล้วให้ทำเป็นไฟล์ migration แยก (YYYYMMDD_NN_fix_...) พร้อม rollback และ backup ก่อนแก้
