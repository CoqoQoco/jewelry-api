-- =============================================
-- Migration: ล็อตเดียวหลายคลัง — ย้ายคลังรายชิ้น + ขาย/จองระบุชิ้น   [TK202610290003]
-- Date: 2026-10-30
-- Description:
--   tbt_stock_unit เป็นความจริงรายคลัง (1 ล็อตมีชิ้นอยู่ได้หลายคลัง) · tbt_stock_piece.location_code = "คลังหลัก"
--   (คลังที่มีชิ้นไม่ SOLD มากสุด) ระบบคำนวณใหม่ทุกครั้งที่ย้าย/จอง/ขาย
--
--   1) tbt_stock_unit เพิ่ม reserved_ref_type / reserved_ref / reserved_line_id
--        reserved_ref_type : SO | CONVERT
--        reserved_ref      : เลข SO (SO) หรือเลขใบแปลง (CONVERT)
--        reserved_line_id  : tbt_sale_order_product.id (เฉพาะ SO)
--      ค่าพวกนี้คงไว้หลังชิ้นกลายเป็น SOLD เพื่อให้ลบ/ยกเลิก invoice แล้วคืนการจองเดิมได้ตรงชิ้น/ตรงบรรทัด
--   2) Backfill ชิ้น RESERVED จากบรรทัด SO ที่ยังไม่ออก invoice (ไม่ใช่ placeholder)
--      ต่อ 1 ล็อต: เรียงบรรทัดตาม id, กระจายตามจำนวน qty ให้ชิ้น RESERVED ตาม unit_no
--      (ข้อเท็จจริง prod: 44 ล็อต / 70 ชิ้น, SUM(qty บรรทัดค้าง) = qty_reserved ทุกล็อต · 0 ใบแปลงค้าง)
--   3) Best-effort: ชิ้น SOLD ที่ sold_ref = เลข invoice -> ใส่ reserved_line_id จากบรรทัดที่ invoice = sold_ref (เรียงตาม id / unit_no)
--   4) tbt_stock_movement_unit : เชื่อม movement กับชิ้นที่เกี่ยวข้อง (ตรวจย้อนรายชิ้น)
--
-- DEPLOY NOTE: ลำดับ deploy: SQL นี้ -> API (TK202610290002 + 003 พร้อมกันได้) -> UI -> APK
--   API ใหม่ไม่บังคับให้ทุกชิ้นอยู่คลังเดียวกับล็อตอีกต่อไป — ต้องรัน SQL ก่อนเสมอ
-- Re-run safety: Idempotent — ADD COLUMN IF NOT EXISTS, CREATE ... IF NOT EXISTS,
--   backfill แตะเฉพาะ unit ที่ reserved_ref_type IS NULL / reserved_line_id IS NULL
-- Rollback: 20261030_01_stock_unit_multi_location_rollback.sql
-- =============================================

BEGIN;

-- =============================================
-- 1. tbt_stock_unit : การจองรายชิ้น
-- =============================================
ALTER TABLE tbt_stock_unit
    ADD COLUMN IF NOT EXISTS reserved_ref_type CHARACTER VARYING;
-- reserved_ref_type: SO, CONVERT

ALTER TABLE tbt_stock_unit
    ADD COLUMN IF NOT EXISTS reserved_ref CHARACTER VARYING;

ALTER TABLE tbt_stock_unit
    ADD COLUMN IF NOT EXISTS reserved_line_id BIGINT;

CREATE INDEX IF NOT EXISTS idx_tbt_stock_unit_reserved_line
    ON tbt_stock_unit (reserved_ref_type, reserved_line_id);

-- =============================================
-- 2. Backfill ชิ้น RESERVED จากบรรทัด SO ที่ยังไม่ออก invoice
-- =============================================
WITH single_lot AS (
    SELECT stock_number, MIN(product_code) AS product_code
    FROM tbt_stock_piece
    GROUP BY stock_number
    HAVING COUNT(*) = 1
),
open_lines AS (
    SELECT sl.stock_number, sl.product_code, sop.id AS line_id, sop.so_number, sop.qty
    FROM tbt_sale_order_product sop
    JOIN single_lot sl ON sl.stock_number = sop.stock_number
    WHERE sop.invoice IS NULL
      AND COALESCE(sop.is_placeholder, FALSE) = FALSE
      AND sop.qty >= 1
),
line_slots AS (
    SELECT ol.stock_number, ol.product_code, ol.line_id, ol.so_number,
           ROW_NUMBER() OVER (PARTITION BY ol.stock_number, ol.product_code ORDER BY ol.line_id, g.n) AS slot_no
    FROM open_lines ol
    CROSS JOIN LATERAL generate_series(1, FLOOR(ol.qty)::int) AS g(n)
),
reserved_units AS (
    SELECT u.id, u.stock_number, u.product_code,
           ROW_NUMBER() OVER (PARTITION BY u.stock_number, u.product_code ORDER BY u.unit_no) AS slot_no
    FROM tbt_stock_unit u
    WHERE u.status = 'RESERVED'
      AND u.reserved_ref_type IS NULL
      AND u.reserved_line_id IS NULL
)
UPDATE tbt_stock_unit u
SET reserved_ref_type = 'SO',
    reserved_ref      = ls.so_number,
    reserved_line_id  = ls.line_id,
    update_date       = now(),
    update_by         = 'MIGRATION'
FROM reserved_units ru
JOIN line_slots ls
  ON ls.stock_number = ru.stock_number
 AND ls.product_code = ru.product_code
 AND ls.slot_no      = ru.slot_no
WHERE u.id = ru.id;

-- =============================================
-- 3. Best-effort: ชิ้น SOLD ที่ขายผ่าน invoice -> ผูกกลับบรรทัด SO
-- =============================================
WITH single_lot AS (
    SELECT stock_number, MIN(product_code) AS product_code
    FROM tbt_stock_piece
    GROUP BY stock_number
    HAVING COUNT(*) = 1
),
sold_lines AS (
    SELECT sl.stock_number, sl.product_code, sop.invoice, sop.id AS line_id, sop.so_number, sop.qty
    FROM tbt_sale_order_product sop
    JOIN single_lot sl ON sl.stock_number = sop.stock_number
    WHERE sop.invoice IS NOT NULL
      AND COALESCE(sop.is_placeholder, FALSE) = FALSE
      AND sop.qty >= 1
),
line_slots AS (
    SELECT s.stock_number, s.product_code, s.invoice, s.line_id, s.so_number,
           ROW_NUMBER() OVER (PARTITION BY s.stock_number, s.product_code, s.invoice ORDER BY s.line_id, g.n) AS slot_no
    FROM sold_lines s
    CROSS JOIN LATERAL generate_series(1, FLOOR(s.qty)::int) AS g(n)
),
sold_units AS (
    SELECT u.id, u.stock_number, u.product_code, u.sold_ref,
           ROW_NUMBER() OVER (PARTITION BY u.stock_number, u.product_code, u.sold_ref ORDER BY u.unit_no) AS slot_no
    FROM tbt_stock_unit u
    WHERE u.status = 'SOLD'
      AND u.sold_ref IS NOT NULL
      AND u.reserved_ref_type IS NULL
      AND u.reserved_line_id IS NULL
)
UPDATE tbt_stock_unit u
SET reserved_ref_type = 'SO',
    reserved_ref      = ls.so_number,
    reserved_line_id  = ls.line_id
FROM sold_units su
JOIN line_slots ls
  ON ls.stock_number = su.stock_number
 AND ls.product_code = su.product_code
 AND ls.invoice      = su.sold_ref
 AND ls.slot_no      = su.slot_no
WHERE u.id = su.id;

-- =============================================
-- 4. tbt_stock_movement_unit
-- =============================================
CREATE TABLE IF NOT EXISTS tbt_stock_movement_unit (
    id              BIGSERIAL NOT NULL,
    movement_id     BIGINT NOT NULL,
    unit_id         BIGINT NOT NULL,
    CONSTRAINT tbt_stock_movement_unit_pk PRIMARY KEY (id),
    CONSTRAINT tbt_stock_movement_unit_movement_fk FOREIGN KEY (movement_id)
        REFERENCES tbt_stock_movement (id),
    CONSTRAINT tbt_stock_movement_unit_unit_fk FOREIGN KEY (unit_id)
        REFERENCES tbt_stock_unit (id)
);

CREATE INDEX IF NOT EXISTS idx_tbt_stock_movement_unit_movement
    ON tbt_stock_movement_unit (movement_id);

CREATE INDEX IF NOT EXISTS idx_tbt_stock_movement_unit_unit
    ON tbt_stock_movement_unit (unit_id);

COMMIT;

-- =============================================
-- Verification
-- =============================================
-- 1) ชิ้น RESERVED ที่ยังไม่ผูกบรรทัด SO / ใบแปลง (คาดหวัง 0 แถว)
-- SELECT stock_number, product_code, unit_no FROM tbt_stock_unit
-- WHERE status = 'RESERVED' AND reserved_line_id IS NULL AND reserved_ref_type IS NULL;
--
-- 2) จำนวนชิ้น RESERVED ที่ผูกแต่ละบรรทัด ต้องเท่ากับ qty ของบรรทัด (คาดหวัง 0 แถว)
-- SELECT sop.id, sop.stock_number, sop.qty, COUNT(u.id) AS linked_units
-- FROM tbt_sale_order_product sop
-- LEFT JOIN tbt_stock_unit u ON u.reserved_ref_type = 'SO' AND u.reserved_line_id = sop.id AND u.status = 'RESERVED'
-- WHERE sop.invoice IS NULL AND COALESCE(sop.is_placeholder, FALSE) = FALSE
-- GROUP BY sop.id, sop.stock_number, sop.qty
-- HAVING COUNT(u.id) <> FLOOR(sop.qty);
--
-- 3) unit กับ piece ตรงกัน (นับ + reserved) (คาดหวัง 0 แถว)
-- SELECT p.stock_number, p.product_code, p.qty, p.qty_reserved,
--        COUNT(u.id) FILTER (WHERE u.status <> 'SOLD') AS units,
--        COUNT(u.id) FILTER (WHERE u.status = 'RESERVED') AS reserved_units
-- FROM tbt_stock_piece p
-- LEFT JOIN tbt_stock_unit u ON u.stock_number = p.stock_number AND u.product_code = p.product_code
-- GROUP BY p.stock_number, p.product_code, p.qty, p.qty_reserved
-- HAVING COUNT(u.id) FILTER (WHERE u.status <> 'SOLD') <> FLOOR(p.qty)
--     OR COUNT(u.id) FILTER (WHERE u.status = 'RESERVED') <> LEAST(FLOOR(p.qty_reserved), FLOOR(p.qty));
--
-- 4) balance รายคลัง = unit รายคลัง (คาดหวัง 0 แถว) — ใช้ SKU จาก piece
-- SELECT COALESCE(a.sku_code, b.sku_code) AS sku_code, COALESCE(a.location_code, b.location_code) AS location_code,
--        COALESCE(a.on_hand, 0) AS unit_on_hand, COALESCE(b.qty_on_hand, 0) AS balance_on_hand,
--        COALESCE(a.reserved, 0) AS unit_reserved, COALESCE(b.qty_reserved, 0) AS balance_reserved
-- FROM (SELECT p.sku_code, u.location_code,
--              COUNT(*) FILTER (WHERE u.status <> 'SOLD') AS on_hand,
--              COUNT(*) FILTER (WHERE u.status = 'RESERVED') AS reserved
--       FROM tbt_stock_unit u
--       JOIN tbt_stock_piece p ON p.stock_number = u.stock_number AND p.product_code = u.product_code
--       GROUP BY p.sku_code, u.location_code) a
-- FULL JOIN tbt_stock_balance b ON b.sku_code = a.sku_code AND b.location_code = a.location_code
-- WHERE COALESCE(a.on_hand, 0) <> COALESCE(b.qty_on_hand, 0)
--    OR COALESCE(a.reserved, 0) <> COALESCE(b.qty_reserved, 0);
--
-- 5) สรุปจำนวนที่ผูก
-- SELECT COUNT(*) FILTER (WHERE status = 'RESERVED' AND reserved_line_id IS NOT NULL) AS reserved_linked,
--        COUNT(*) FILTER (WHERE status = 'SOLD' AND reserved_line_id IS NOT NULL) AS sold_linked,
--        COUNT(*) FILTER (WHERE status = 'SOLD') AS sold_total
-- FROM tbt_stock_unit;
