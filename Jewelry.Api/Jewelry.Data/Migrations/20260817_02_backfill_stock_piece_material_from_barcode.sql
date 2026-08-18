-- =============================================
-- Migration: Backfill weight/weight_unit/price on tbt_stock_piece_material
-- Date: 2026-08-17
-- Description:
--   ticket TK202608140001 — หน้ารับสินค้างานผลิตแก้ไขวัสดุด้วย field ที่ DTO เดิมไม่รับ
--   (qtyWeight/qtyWeightUnit/qtyPrice/qtyWeightPrice) ทำให้ weight/weight_unit/price
--   เป็น NULL ทุกแถวที่มาจาก flow นี้ (production receipt) — โค้ด API แก้แล้วแยกต่างหาก
--   script นี้ backfill ข้อมูลเก่าย้อนหลังโดยอ่านค่าจาก type_barcode (เก็บน้ำหนัก/หน่วย
--   ในรูปแบบ "<น้ำหนัก> g." หรือ "<น้ำหนัก> ct.") และ json_breakdown ของ
--   tbt_stock_product_receipt_plan (เก็บ qtyPrice/qtyWeightPrice ต่อหน่วย) เพื่อคำนวณ price
--   validate กับ prod (read-only) แล้ว: เติม weight ได้ 1,215/1,215 แถว, price ได้ 1,204/1,215
--   แถว (เหลือ ~11 แถวที่จับคู่ breakdown ไม่ได้ตาม type/typeName) — spot-check ตรงกับ
--   type_barcode ทุกจุดที่สุ่มตรวจ
--   กระทบเฉพาะแถวที่ p.receipt_type = 'production' และ m.weight IS NULL เท่านั้น
-- =============================================

WITH target AS (
    SELECT m.id, m.type, m.type_name, m.qty, m.type_barcode, p.receipt_number
    FROM tbt_stock_piece_material m
    JOIN tbt_stock_piece p ON p.stock_number = m.stock_number
    WHERE p.receipt_type = 'production'
      AND m.weight IS NULL
),
linked AS (
    SELECT t.*, pl.json_breakdown
    FROM target t
    JOIN tbt_stock_product_receipt_item i ON i.stock_receipt_number = t.receipt_number
    JOIN tbt_stock_product_receipt_plan pl ON pl.running = i.running
),
bd AS (
    SELECT l.id,
           MAX((e ->> 'qtyPrice')::NUMERIC)       AS bd_qty_price,
           MAX((e ->> 'qtyWeightPrice')::NUMERIC) AS bd_weight_price,
           MAX((e ->> 'qtyWeight')::NUMERIC)      AS bd_qty_weight
    FROM linked l
    CROSS JOIN LATERAL jsonb_array_elements(l.json_breakdown::JSONB) AS e
    WHERE e ->> 'type' = l.type
      AND COALESCE(e ->> 'typeName', '') = COALESCE(l.type_name, '')
    GROUP BY l.id
),
calc AS (
    SELECT l.id,
           COALESCE(
               NULLIF(substring(l.type_barcode FROM '([0-9]+(?:\.[0-9]+)?)\s*(?:g\.|ct\.)'), '')::NUMERIC,
               bd.bd_qty_weight
           ) AS new_weight,
           CASE
               WHEN l.type_barcode ~ '[0-9]\s*g\.'   THEN 'g.'
               WHEN l.type_barcode ~ '[0-9]\s*ct\.'  THEN 'ct.'
               WHEN l.type IN ('Gold', 'Silver')     THEN 'g.'
               WHEN l.type IN ('Gem', 'Diamond')     THEN 'ct.'
               ELSE NULL
           END AS new_weight_unit,
           bd.bd_qty_price,
           bd.bd_weight_price
    FROM linked l
    LEFT JOIN bd ON bd.id = l.id
)
UPDATE tbt_stock_piece_material m
SET weight      = c.new_weight,
    weight_unit = COALESCE(m.weight_unit, c.new_weight_unit),
    price       = CASE
                      WHEN (COALESCE(m.qty, 0) * COALESCE(c.bd_qty_price, 0)
                          + COALESCE(c.new_weight, 0) * COALESCE(c.bd_weight_price, 0)) > 0
                      THEN ROUND(COALESCE(m.qty, 0) * COALESCE(c.bd_qty_price, 0)
                          + COALESCE(c.new_weight, 0) * COALESCE(c.bd_weight_price, 0), 2)
                      ELSE m.price
                  END,
    update_date = NOW(),
    update_by   = 'SYSTEM-TK202608140001'
FROM calc c
WHERE m.id = c.id
  AND m.weight IS NULL
  AND c.new_weight IS NOT NULL;

-- =============================================
-- Verify (run after migration):
-- =============================================

-- 1. นับผลหลัง run — ควรเหลือ weight IS NULL = 0 แถว
--    (price IS NULL คาดว่าเหลือ ~11 แถว คือแถวที่จับคู่ json_breakdown ไม่ได้)
-- SELECT
--     COUNT(*) FILTER (WHERE m.weight IS NULL) AS remaining_weight_null,
--     COUNT(*) FILTER (WHERE m.price IS NULL)  AS remaining_price_null
-- FROM tbt_stock_piece_material m
-- JOIN tbt_stock_piece p ON p.stock_number = m.stock_number
-- WHERE p.receipt_type = 'production';

-- 2. spot-check เทียบ type_barcode กับ weight/weight_unit/price ที่เติม
-- SELECT stock_number, type, type_name, type_barcode, qty, weight, weight_unit, price
-- FROM tbt_stock_piece_material
-- WHERE stock_number IN ('DK-20G-237', 'DK-20G-235')
-- ORDER BY stock_number, type;
