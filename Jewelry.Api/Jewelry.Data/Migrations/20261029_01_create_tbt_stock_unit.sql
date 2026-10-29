-- =============================================
-- Migration: Stock unit (1 RFID tag = 1 physical piece)   [TK202610290002]
-- Date: 2026-10-29
-- Description:
--   tbt_stock_piece = ล็อต (คีย์ผสม stock_number + product_code, มี qty) — ทอง qty 1, เงินได้ถึงหลักสิบชิ้นต่อล็อต
--   tbt_stock_unit  = "ชิ้นจริง" 1 แถวต่อ 1 ชิ้นในล็อต (unit_no 1..N) เก็บคลัง/สถานะ/ใบรับ/ใบขายรายชิ้น
--   tbt_stock_piece_rfid ผูกแท็กกับ unit (unit_id) แทนการผูกกับทั้งล็อต
--
--   tbt_stock_unit.status : IN_STOCK | RESERVED | SOLD
--   tbt_stock_piece_rfid.unbind_reason : MANUAL (ยกเลิกผูกเอง) | SOLD (ปลดอัตโนมัติเมื่อ unit ถูกขาย/ใช้)
--   ดัชนี unique ของแท็ก: epc (active) คงเดิม + unit_id (active) แทน (stock_number, product_code) (active)
--
-- Backfill:
--   ทุก piece ที่ status <> 'SOLD' และ qty > 0 และยังไม่มี unit -> สร้าง FLOOR(qty) แถว (unit_no 1..qty)
--   อยู่คลังเดียวกับ piece, qty_reserved แถวแรก = RESERVED ที่เหลือ = IN_STOCK
--   receipt_number/receipt_date คัดลอกจาก piece · piece ที่ SOLD / qty 0 ไม่สร้าง unit
--   แท็ก active เดิม -> ผูกกับ unit_no 1 ของล็อต (ถ้าหา unit ไม่ได้ เช่นล็อต SOLD -> ปลดแท็กเป็น unbind_reason = 'SOLD')
--
-- Expected rows (ประมาณการ ณ 2026-10-29): ~44,800 units
--   ตัวเลขจริง = SUM(FLOOR(qty)) ของ piece ที่ status <> 'SOLD' AND qty > 0 (ดู verification ท้ายไฟล์)
--
-- DEPLOY NOTE: หยุดรับสินค้า/ขาย/ย้ายคลัง 5-10 นาทีระหว่างรัน (ถ้ามีการเปลี่ยน qty ระหว่าง backfill unit จะไม่ตรงกับ piece)
--   ลำดับ deploy: SQL นี้ -> API -> UI -> APK
-- Re-run safety: Idempotent — CREATE ... IF NOT EXISTS, ADD COLUMN IF NOT EXISTS, backfill ข้ามล็อตที่มี unit แล้ว
-- Rollback: 20261029_01_rollback_tbt_stock_unit.sql
-- =============================================

BEGIN;

-- =============================================
-- 1. tbt_stock_unit
-- =============================================
CREATE TABLE IF NOT EXISTS tbt_stock_unit (
    id              BIGSERIAL NOT NULL,
    stock_number    CHARACTER VARYING NOT NULL,
    product_code    CHARACTER VARYING NOT NULL,
    unit_no         INT NOT NULL,
    location_code   CHARACTER VARYING NOT NULL,
    status          CHARACTER VARYING NOT NULL DEFAULT 'IN_STOCK',
    -- Status: IN_STOCK, RESERVED, SOLD
    receipt_number  CHARACTER VARYING,
    receipt_date    TIMESTAMPTZ,
    sold_ref        CHARACTER VARYING,
    sold_date       TIMESTAMPTZ,
    create_date     TIMESTAMPTZ NOT NULL,
    create_by       CHARACTER VARYING NOT NULL,
    update_date     TIMESTAMPTZ,
    update_by       CHARACTER VARYING,
    CONSTRAINT tbt_stock_unit_pk PRIMARY KEY (id),
    CONSTRAINT tbt_stock_unit_piece_fk FOREIGN KEY (stock_number, product_code)
        REFERENCES tbt_stock_piece (stock_number, product_code),
    CONSTRAINT tbt_stock_unit_location_fk FOREIGN KEY (location_code)
        REFERENCES tbm_stock_location (code),
    CONSTRAINT tbt_stock_unit_status_chk CHECK (status IN ('IN_STOCK', 'RESERVED', 'SOLD'))
);

CREATE UNIQUE INDEX IF NOT EXISTS tbt_stock_unit_lot_no_uq
    ON tbt_stock_unit (stock_number, product_code, unit_no);

CREATE INDEX IF NOT EXISTS idx_tbt_stock_unit_lot_status
    ON tbt_stock_unit (stock_number, product_code, status);

CREATE INDEX IF NOT EXISTS idx_tbt_stock_unit_receipt
    ON tbt_stock_unit (receipt_number);

CREATE INDEX IF NOT EXISTS idx_tbt_stock_unit_location_status
    ON tbt_stock_unit (location_code, status);

-- =============================================
-- 2. tbt_stock_piece_rfid : ผูกกับ unit
-- =============================================
ALTER TABLE tbt_stock_piece_rfid
    ADD COLUMN IF NOT EXISTS unit_id BIGINT;

ALTER TABLE tbt_stock_piece_rfid
    ADD COLUMN IF NOT EXISTS unbind_reason CHARACTER VARYING;
-- unbind_reason: MANUAL, SOLD

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'tbt_stock_piece_rfid_unit_fk'
    ) THEN
        ALTER TABLE tbt_stock_piece_rfid
            ADD CONSTRAINT tbt_stock_piece_rfid_unit_fk FOREIGN KEY (unit_id)
            REFERENCES tbt_stock_unit (id);
    END IF;
END $$;

-- =============================================
-- 3. Backfill units จาก piece (ข้ามล็อตที่มี unit แล้ว)
-- =============================================
INSERT INTO tbt_stock_unit
    (stock_number, product_code, unit_no, location_code, status,
     receipt_number, receipt_date, create_date, create_by)
SELECT p.stock_number,
       p.product_code,
       g.n,
       p.location_code,
       CASE WHEN g.n <= LEAST(GREATEST(FLOOR(p.qty_reserved), 0), FLOOR(p.qty))
            THEN 'RESERVED' ELSE 'IN_STOCK' END,
       p.receipt_number,
       p.receipt_date,
       now(),
       'MIGRATION'
FROM tbt_stock_piece p
CROSS JOIN LATERAL generate_series(1, FLOOR(p.qty)::int) AS g(n)
WHERE COALESCE(p.status, '') <> 'SOLD'
  AND p.qty > 0
  AND NOT EXISTS (
      SELECT 1 FROM tbt_stock_unit u
      WHERE u.stock_number = p.stock_number AND u.product_code = p.product_code
  );

-- =============================================
-- 4. แท็ก active เดิม -> unit_no 1 ของล็อต
-- =============================================
UPDATE tbt_stock_piece_rfid r
SET unit_id = u.id
FROM tbt_stock_unit u
WHERE r.is_active
  AND r.unit_id IS NULL
  AND u.stock_number = r.stock_number
  AND u.product_code = r.product_code
  AND u.unit_no = 1
  AND u.status <> 'SOLD';

-- แท็ก active ที่หา unit ไม่ได้ (ล็อตขายหมดแล้ว) -> ปลดอัตโนมัติ
UPDATE tbt_stock_piece_rfid
SET is_active = FALSE,
    unbind_reason = 'SOLD',
    update_date = now(),
    update_by = 'MIGRATION'
WHERE is_active AND unit_id IS NULL;

-- =============================================
-- 5. ดัชนี unique ของแท็ก: ต่อ unit แทนต่อล็อต (epc active คงเดิม)
-- =============================================
DROP INDEX IF EXISTS idx_tbt_stock_piece_rfid_piece_active;

CREATE UNIQUE INDEX IF NOT EXISTS idx_tbt_stock_piece_rfid_unit_active
    ON tbt_stock_piece_rfid (unit_id)
    WHERE is_active;

CREATE INDEX IF NOT EXISTS idx_tbt_stock_piece_rfid_lot
    ON tbt_stock_piece_rfid (stock_number, product_code);

COMMIT;

-- =============================================
-- Verification (ข้อ 1-2 ควรได้ 0 แถว)
-- =============================================
-- 1) จำนวน unit ไม่ SOLD ต่อล็อต ต้องเท่ากับ qty ของ piece
-- SELECT p.stock_number, p.product_code, p.qty, COUNT(u.id) AS units
-- FROM tbt_stock_piece p
-- LEFT JOIN tbt_stock_unit u ON u.stock_number = p.stock_number AND u.product_code = p.product_code AND u.status <> 'SOLD'
-- GROUP BY p.stock_number, p.product_code, p.qty
-- HAVING COUNT(u.id) <> FLOOR(p.qty);
--
-- 2) จำนวน RESERVED ต่อล็อต ต้องเท่ากับ qty_reserved
-- SELECT p.stock_number, p.product_code, p.qty_reserved, COUNT(u.id) AS reserved_units
-- FROM tbt_stock_piece p
-- LEFT JOIN tbt_stock_unit u ON u.stock_number = p.stock_number AND u.product_code = p.product_code AND u.status = 'RESERVED'
-- GROUP BY p.stock_number, p.product_code, p.qty_reserved
-- HAVING COUNT(u.id) <> LEAST(FLOOR(p.qty_reserved), FLOOR(p.qty));
--
-- 3) จำนวน unit ทั้งหมด (เทียบ expected ~44,800)
-- SELECT COUNT(*) AS total_units,
--        COUNT(*) FILTER (WHERE status = 'RESERVED') AS reserved_units
-- FROM tbt_stock_unit;
