-- =============================================
-- Migration: Fix stock balance location drift (13 SKU ที่ MAIN ติดลบ / คลังอื่นบวกเกิน)
-- Date: 2026-09-16
-- Description: tbt_stock_balance คือยอดสรุปต่อ SKU-ต่อ-คลัง (roll-up จาก tbt_stock_piece)
--              พบ 13 SKU ที่ MAIN = -1 และอีกคลังหนึ่ง = +1 พร้อมกัน ทั้งที่ทุกชิ้นของ SKU
--              นั้นถูกขายหมดแล้ว (piece qty = 0 ทุกชิ้น สถานะ SOLD) ยอด balance ที่ถูกต้อง
--              ของทั้งสองแถวจึงต้องเป็น 0 ทั้งคู่ ไม่ใช่ -1/+1
--              ตัวอย่างที่ตรวจสอบละเอียด: SKU-BF367M492D7YL4 / ชิ้น DK-18K-1XR-3623
--              RECEIPT เข้า MAIN -> RESERVE -> SALE (INV260623001, ตอนนั้น balance MAIN = 0
--              ถูกต้องแล้ว) -> ต่อมาวันที่ 2026-07-15 มีคนใช้หน้าจอย้ายคลัง (MoveLocation)
--              กับชิ้นที่ขายไปแล้วนี้ (ชิ้นที่ SOLD ไม่ควรย้ายคลังได้) ทำให้ MAIN ติดลบ -1
--              และ STOCK01 บวกเกิน +1 อีก 12 SKU ที่เหลือมีรูปแบบเดียวกัน
-- History: ตรวจสอบ read-only บน prod วันที่ 16 ก.ย. 2026 — ทุก piece ของ 13 SKU นี้อยู่ใน
--          สถานะ SOLD หมดแล้ว (qty = 0 ทุกชิ้น) และ qty_reserved ของทุกแถว balance ที่จะแก้
--          เป็น 0 อยู่แล้ว (ไม่มีการจองค้าง) โค้ด StockMovementService.MoveLocation ได้แก้ไข
--          ให้ปฏิเสธการย้ายคลังชิ้นที่ SOLD/qty<=0 แล้ว (commit แยกต่างหาก) — สคริปต์นี้แก้
--          เฉพาะข้อมูลเก่าที่ค้างผิดอยู่ก่อนโค้ดจะถูกแก้เท่านั้น ไม่มีการเปลี่ยนโค้ดในนี้
-- Scope: แก้ tbt_stock_balance ของ 13 SKU ต่อไปนี้ ตั้ง qty_on_hand = 0 ทั้งแถว MAIN และแถว
--        คลังปลายทางที่ติดยอดผิด (qty_reserved ไม่แตะ เพราะเป็น 0 อยู่แล้วทุกแถว):
--          SKU-520007RU7YL1        : MAIN -1 / STOCK08 +1
--          SKU-520058SD7WL         : MAIN -1 / STOCK13 +1
--          SKU-BF354M101D7WL1      : MAIN -1 / STOCK01 +1
--          SKU-BF354M101D7YL2      : MAIN -1 / STOCK01 +1
--          SKU-BF358M480D7WL       : MAIN -1 / STOCK01 +1
--          SKU-BF358M480D7YL2      : MAIN -1 / STOCK01 +1
--          SKU-BF367M492D7YL4      : MAIN -1 / STOCK01 +1
--          SKU-BF367M493D7YL2      : MAIN -1 / STOCK01 +1
--          SKU-BF367M552D7YL3      : MAIN -1 / STOCK01 +1
--          SKU-BF367M553D7YL1      : MAIN -1 / STOCK01 +1
--          SKU-CR09979SD7YL4       : MAIN -1 / STOCK08 +1
--          SKU-R02326RD7W2         : MAIN -1 / STOCK08 +1
--          SKU-R17834R7YH6         : MAIN -1 / STOCK14 +1
--        qty_available เป็น generated column (qty_on_hand - qty_reserved) ห้าม UPDATE ตรงๆ
--        ปล่อยให้ Postgres คำนวณเองเสมอ
-- ⚠️ นอกขอบเขต (ไม่แก้ในสคริปต์นี้ เป็นคนละปัญหา): SKU-S520025RU7YL1 @ STOCK01-2
--    (on_hand 0 แต่ reserved 1) — เป็นเคสชิ้น DK-18K-1XR-4293 ที่ยังรอร้านยืนยันว่าผูกกับ
--    การขายใบไหน ต้องแก้แยกสคริปต์ทีหลังเมื่อร้านยืนยันแล้ว
-- Not touched: tbt_stock_piece (ทุกชิ้นของ 13 SKU นี้เป็น SOLD/qty=0 ถูกต้องอยู่แล้ว ไม่แตะ),
--              tbt_stock_movement (ไม่เพิ่ม/ไม่แก้ ledger ประวัติการย้ายคลังเดิม),
--              tbt_sale_invoice_header/tbt_sale_order (ไม่แตะยอดเงิน/สถานะบิล),
--              SKU-S520025RU7YL1 (ดู ⚠️ ด้านบน)
-- Re-run safe: ทุก UPDATE guard ด้วย sku_code + location_code + qty_on_hand เดิมเป๊ะ
--              (-1 สำหรับแถว MAIN, +1 สำหรับแถวคลังปลายทาง) + qty_reserved = 0 รันซ้ำหลังแก้
--              แล้วค่าจะไม่ตรง WHERE อีกต่อไป จึง no-op และจะไม่ไปทับแถวที่เปลี่ยนไปจาก
--              สาเหตุอื่นระหว่างทาง (เช่นมีการจองใหม่ทำให้ qty_reserved เปลี่ยน)
-- =============================================

-- =============================================
-- BEFORE — สถานะปัจจุบันของ balance ทั้ง 26 แถว (13 SKU x 2 คลัง) เทียบกับผลรวม piece ที่ยังไม่ SOLD
-- =============================================
SELECT
    b.sku_code,
    b.location_code,
    b.qty_on_hand,
    b.qty_reserved,
    COALESCE(p.piece_qty_sum, 0) AS piece_qty_sum,
    b.qty_on_hand - COALESCE(p.piece_qty_sum, 0) AS diff
FROM tbt_stock_balance b
LEFT JOIN (
    SELECT sku_code, location_code,
           SUM(qty) FILTER (WHERE status <> 'SOLD') AS piece_qty_sum
    FROM tbt_stock_piece
    GROUP BY sku_code, location_code
) p ON p.sku_code = b.sku_code AND p.location_code = b.location_code
WHERE b.sku_code IN (
    'SKU-520007RU7YL1', 'SKU-520058SD7WL', 'SKU-BF354M101D7WL1',
    'SKU-BF354M101D7YL2', 'SKU-BF358M480D7WL', 'SKU-BF358M480D7YL2',
    'SKU-BF367M492D7YL4', 'SKU-BF367M493D7YL2', 'SKU-BF367M552D7YL3',
    'SKU-BF367M553D7YL1', 'SKU-CR09979SD7YL4', 'SKU-R02326RD7W2',
    'SKU-R17834R7YH6'
)
ORDER BY b.sku_code, b.location_code;

-- ยืนยันว่าทุกชิ้นของ 13 SKU นี้ SOLD หมดแล้วจริง (ต้องไม่มีชิ้นไหน status <> 'SOLD')
SELECT sku_code, stock_number, location_code, qty, qty_reserved, status
FROM tbt_stock_piece
WHERE sku_code IN (
    'SKU-520007RU7YL1', 'SKU-520058SD7WL', 'SKU-BF354M101D7WL1',
    'SKU-BF354M101D7YL2', 'SKU-BF358M480D7WL', 'SKU-BF358M480D7YL2',
    'SKU-BF367M492D7YL4', 'SKU-BF367M493D7YL2', 'SKU-BF367M552D7YL3',
    'SKU-BF367M553D7YL1', 'SKU-CR09979SD7YL4', 'SKU-R02326RD7W2',
    'SKU-R17834R7YH6'
)
ORDER BY sku_code, stock_number;

-- =============================================
-- FIX — สำรองข้อมูลก่อน แล้วตั้ง qty_on_hand = 0 ทั้งสองแถวของ 13 SKU
-- =============================================
BEGIN;

CREATE TABLE IF NOT EXISTS bak_20260916_balance_location_drift AS
SELECT b.*
FROM tbt_stock_balance b
JOIN (VALUES
    ('SKU-520007RU7YL1', 'MAIN'),      ('SKU-520007RU7YL1', 'STOCK08'),
    ('SKU-520058SD7WL', 'MAIN'),       ('SKU-520058SD7WL', 'STOCK13'),
    ('SKU-BF354M101D7WL1', 'MAIN'),    ('SKU-BF354M101D7WL1', 'STOCK01'),
    ('SKU-BF354M101D7YL2', 'MAIN'),    ('SKU-BF354M101D7YL2', 'STOCK01'),
    ('SKU-BF358M480D7WL', 'MAIN'),     ('SKU-BF358M480D7WL', 'STOCK01'),
    ('SKU-BF358M480D7YL2', 'MAIN'),    ('SKU-BF358M480D7YL2', 'STOCK01'),
    ('SKU-BF367M492D7YL4', 'MAIN'),    ('SKU-BF367M492D7YL4', 'STOCK01'),
    ('SKU-BF367M493D7YL2', 'MAIN'),    ('SKU-BF367M493D7YL2', 'STOCK01'),
    ('SKU-BF367M552D7YL3', 'MAIN'),    ('SKU-BF367M552D7YL3', 'STOCK01'),
    ('SKU-BF367M553D7YL1', 'MAIN'),    ('SKU-BF367M553D7YL1', 'STOCK01'),
    ('SKU-CR09979SD7YL4', 'MAIN'),     ('SKU-CR09979SD7YL4', 'STOCK08'),
    ('SKU-R02326RD7W2', 'MAIN'),       ('SKU-R02326RD7W2', 'STOCK08'),
    ('SKU-R17834R7YH6', 'MAIN'),       ('SKU-R17834R7YH6', 'STOCK14')
) AS v(sku_code, location_code)
    ON b.sku_code = v.sku_code AND b.location_code = v.location_code;

-- แถว MAIN ของทั้ง 13 SKU — ติดลบผี -1 -> 0
UPDATE tbt_stock_balance b
SET qty_on_hand = 0,
    update_by = 'system-fix',
    update_date = now()
FROM (VALUES
    ('SKU-520007RU7YL1', 'MAIN'), ('SKU-520058SD7WL', 'MAIN'),
    ('SKU-BF354M101D7WL1', 'MAIN'), ('SKU-BF354M101D7YL2', 'MAIN'),
    ('SKU-BF358M480D7WL', 'MAIN'), ('SKU-BF358M480D7YL2', 'MAIN'),
    ('SKU-BF367M492D7YL4', 'MAIN'), ('SKU-BF367M493D7YL2', 'MAIN'),
    ('SKU-BF367M552D7YL3', 'MAIN'), ('SKU-BF367M553D7YL1', 'MAIN'),
    ('SKU-CR09979SD7YL4', 'MAIN'), ('SKU-R02326RD7W2', 'MAIN'),
    ('SKU-R17834R7YH6', 'MAIN')
) AS v(sku_code, location_code)
WHERE b.sku_code = v.sku_code
  AND b.location_code = v.location_code
  AND b.qty_on_hand = -1
  AND b.qty_reserved = 0;

-- แถวคลังปลายทางของทั้ง 13 SKU — บวกเกินผี +1 -> 0
UPDATE tbt_stock_balance b
SET qty_on_hand = 0,
    update_by = 'system-fix',
    update_date = now()
FROM (VALUES
    ('SKU-520007RU7YL1', 'STOCK08'), ('SKU-520058SD7WL', 'STOCK13'),
    ('SKU-BF354M101D7WL1', 'STOCK01'), ('SKU-BF354M101D7YL2', 'STOCK01'),
    ('SKU-BF358M480D7WL', 'STOCK01'), ('SKU-BF358M480D7YL2', 'STOCK01'),
    ('SKU-BF367M492D7YL4', 'STOCK01'), ('SKU-BF367M493D7YL2', 'STOCK01'),
    ('SKU-BF367M552D7YL3', 'STOCK01'), ('SKU-BF367M553D7YL1', 'STOCK01'),
    ('SKU-CR09979SD7YL4', 'STOCK08'), ('SKU-R02326RD7W2', 'STOCK08'),
    ('SKU-R17834R7YH6', 'STOCK14')
) AS v(sku_code, location_code)
WHERE b.sku_code = v.sku_code
  AND b.location_code = v.location_code
  AND b.qty_on_hand = 1
  AND b.qty_reserved = 0;

COMMIT;

-- =============================================
-- AFTER — รันซ้ำ query BEFORE เพื่อเทียบผล + ตรวจว่ายังผิดอยู่ไหม
-- =============================================
SELECT
    b.sku_code,
    b.location_code,
    b.qty_on_hand,
    b.qty_reserved,
    COALESCE(p.piece_qty_sum, 0) AS piece_qty_sum,
    b.qty_on_hand - COALESCE(p.piece_qty_sum, 0) AS diff
FROM tbt_stock_balance b
LEFT JOIN (
    SELECT sku_code, location_code,
           SUM(qty) FILTER (WHERE status <> 'SOLD') AS piece_qty_sum
    FROM tbt_stock_piece
    GROUP BY sku_code, location_code
) p ON p.sku_code = b.sku_code AND p.location_code = b.location_code
WHERE b.sku_code IN (
    'SKU-520007RU7YL1', 'SKU-520058SD7WL', 'SKU-BF354M101D7WL1',
    'SKU-BF354M101D7YL2', 'SKU-BF358M480D7WL', 'SKU-BF358M480D7YL2',
    'SKU-BF367M492D7YL4', 'SKU-BF367M493D7YL2', 'SKU-BF367M552D7YL3',
    'SKU-BF367M553D7YL1', 'SKU-CR09979SD7YL4', 'SKU-R02326RD7W2',
    'SKU-R17834R7YH6'
)
ORDER BY b.sku_code, b.location_code;

-- ตรวจว่ายังผิดอยู่ไหม — ต้องไม่มีแถวใดเลยถ้าแก้ถูกต้อง (balance ต้องเท่ากับผลรวม qty ของ
-- ชิ้นที่ยังไม่ SOLD พอดีทุกแถวของ 13 SKU นี้)
SELECT
    b.sku_code,
    b.location_code,
    b.qty_on_hand,
    b.qty_reserved,
    COALESCE(p.piece_qty_sum, 0) AS piece_qty_sum,
    b.qty_on_hand - COALESCE(p.piece_qty_sum, 0) AS diff
FROM tbt_stock_balance b
LEFT JOIN (
    SELECT sku_code, location_code,
           SUM(qty) FILTER (WHERE status <> 'SOLD') AS piece_qty_sum
    FROM tbt_stock_piece
    GROUP BY sku_code, location_code
) p ON p.sku_code = b.sku_code AND p.location_code = b.location_code
WHERE b.sku_code IN (
    'SKU-520007RU7YL1', 'SKU-520058SD7WL', 'SKU-BF354M101D7WL1',
    'SKU-BF354M101D7YL2', 'SKU-BF358M480D7WL', 'SKU-BF358M480D7YL2',
    'SKU-BF367M492D7YL4', 'SKU-BF367M493D7YL2', 'SKU-BF367M552D7YL3',
    'SKU-BF367M553D7YL1', 'SKU-CR09979SD7YL4', 'SKU-R02326RD7W2',
    'SKU-R17834R7YH6'
)
  AND b.qty_on_hand - COALESCE(p.piece_qty_sum, 0) <> 0;
