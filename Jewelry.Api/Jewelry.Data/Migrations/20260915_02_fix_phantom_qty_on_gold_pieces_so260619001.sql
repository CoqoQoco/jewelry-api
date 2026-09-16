-- =============================================
-- Migration: Fix phantom qty on gold pieces tied to SO260619001
-- Date: 2026-09-15
-- Description: SO260619001 (ลูกค้า NGG Enterprise สร้าง 19 มิ.ย. โดย KAMONWAN
--              9 แบบสร้อยข้อมือทอง 18K ยืนยันจำนวนใบละ 5) ยืนยัน/ออกบิล qty 5
--              ทับชิ้นทองเดี่ยว (ทอง 1 stock_number = 1 ชิ้นจริง ไม่ใช่ล็อตแบบเงิน)
--              ตั้งแต่ก่อนที่ระบบจะมีคอลัมน์ qty บน tbt_stock_piece — ยอดจึงผิดมาตั้งแต่ต้น
--              วันที่ 08 ก.ย. migration 20260908_01 เพิ่มคอลัมน์ qty แล้ว backfill แถว
--              SOLD เดิมเป็น qty=0 (ตามสถานะตอนนั้น) วันที่ 11 ก.ย. KHAMSORN ลบใบแจ้งหนี้
--              INV260622002 เกิด movement RETURN +5 ต่อชิ้น ทำให้ qty เด้งกลับเป็น 5
--              (ผี — ของจริงมีชิ้นเดียว) หลังจากนั้น 3 ใน 9 ชิ้นถูกขายจริงอีกคนละ 1
--              ผ่าน INV260911003 (SO260911001) และ INV260912004 (SO260912001)
--              โค้ดหัก piece.Qty -= 1 ตามปกติ ทำให้เหลือ qty=4 (ควรเป็น 0 เพราะของหมดแล้ว)
--              รวมจำนวนผี (ส่วนเกินจากของจริง) ทั้งหมด 9 ชิ้น = 36 หน่วย
--              (6 ชิ้น x ส่วนเกิน 4 + 3 ชิ้น x ส่วนเกิน 4 = 36)
-- History: ตรวจสอบ read-only บน prod วันที่ 15 ก.ย. 2026 แล้ว movement ledger ระดับชิ้น
--          (RECEIPT + RETURN − SALE ต่อ stock_number) ถูกต้องอยู่แล้ว (สุทธิ 0 หรือ 1)
--          ปัญหาอยู่ที่คอลัมน์ qty บน tbt_stock_piece และ qty_on_hand บน tbt_stock_balance
--          เท่านั้น ยืนยันแล้วว่าไม่มีชิ้นทอง/เงินอื่นนอกเหนือ 9 ชิ้นนี้ที่ qty > 1 ทั่วระบบ
--          (ยกเว้นเลข DK-SIL- ซึ่งเป็นล็อตเงินที่ตั้งใจให้ qty > 1 ได้) และไม่มีแถว
--          tbt_sale_order_product ที่ qty > 1 บนเลขที่ไม่ใช่ DK-SIL- เหลืออยู่แล้ว
-- Scope: แก้เฉพาะ 9 ชิ้นทองที่ผูกกับ SO260619001 เท่านั้น
--          กลุ่ม A (6 ชิ้น @ MAIN) — ยังไม่เคยถูกขายจริงเลยสักชิ้น: qty 5 -> 1, คง IN_STOCK
--            DK-18K-20A-27962 / DK-18K-1XR-3772 / DK-18K-1XR-3774 /
--            DK-18K-1XR-3775 / DK-18K-1XR-3955 / DK-18K-1XR-4004
--          กลุ่ม B (3 ชิ้น @ STOCK01) — ถูกขายจริงไปแล้วคนละ 1: qty 4 -> 0, IN_STOCK -> SOLD
--            DK-18K-20A-26224 / DK-18K-20A-26237 / DK-18K-20A-26487
--          tbt_stock_balance: กลุ่ม A ที่ MAIN ค่าถูกอยู่แล้ว (qty_on_hand=1) ไม่แก้
--                             กลุ่ม B ที่ MAIN: -5 -> 0, กลุ่ม B ที่ STOCK01: 5 -> 0
--        นอกขอบเขต (ไม่แก้ในสคริปต์นี้ เป็นคนละปัญหา): มีอีกราว 13 คู่ SKU ที่ยอด balance
--        ผิดคลัง (MAIN -1 / คลังอื่น +1 แต่รวมสุทธิ 0 ชิ้น SOLD แล้ว) กับอีก 1 แถว
--        SKU-S520025RU7YL1 @ STOCK01-2 (on_hand 0 แต่ reserved 1) — ต้องแก้แยกทีหลัง
-- ⚠️ คำเตือนสำคัญ: ห้ามรันสคริปต์นี้จนกว่าร้านจะนับของจริงยืนยันก่อนว่า
--    6 เลข 27962 / 3772 / 3774 / 3775 / 3955 / 4004 มีสร้อยข้อมืออยู่จริงเลขละ 1 เส้น
--    และ 3 เลข 26224 / 26237 / 26487 ของหมดแล้วจริง (ขายไปกับ INV260911003 / INV260912004)
--    ถ้านับแล้วไม่ตรง ให้หยุดแล้วแจ้ง dev ทันที ห้ามรันสคริปต์ต่อ
-- Not touched: tbt_stock_movement (ledger ระดับชิ้นถูกต้องอยู่แล้ว ไม่มีการเพิ่ม movement
--              type ใหม่หรือแก้ประวัติเดิม), SO260619001 data JSON (บรรทัดสินค้ายังโชว์
--              qty 5 ตามที่คีย์ไว้ — พนักงานไปแก้เองในหน้าเว็บ 3 บรรทัดในนั้นชี้ไปที่ชิ้น
--              ที่ขายไปแล้ว), tbt_sale_invoice_header/tbt_sale_order (ไม่แตะยอดเงิน/สถานะบิล),
--              tbt_sale_order_product (ตรวจแล้วไม่มี qty > 1 ผิดปกติเหลืออยู่)
-- Re-run safe: ทุก UPDATE guard ด้วยค่าเดิมเป๊ะ (stock_number/location_code + qty เดิม +
--              qty_reserved = 0 + status = 'IN_STOCK' สำหรับ piece, sku_code/location_code +
--              qty_on_hand เดิม + qty_reserved = 0 สำหรับ balance) รันซ้ำหลังแก้แล้ว
--              ค่าจะไม่ตรง WHERE อีกต่อไป จึง no-op และจะไม่ไปทับแถวที่เปลี่ยนไปจากสาเหตุอื่น
--              (เช่นถูกขายเพิ่มที่งานออกร้าน) เพราะ qty เดิมจะไม่ตรง guard แล้ว
-- =============================================

-- =============================================
-- BEFORE — สถานะปัจจุบันของ 9 ชิ้น / balance ของ 9 SKU / ledger ระดับชิ้น
-- =============================================
SELECT stock_number, sku_code, location_code, qty, qty_reserved, status, update_by, update_date
FROM tbt_stock_piece
WHERE stock_number IN (
    'DK-18K-20A-27962', 'DK-18K-1XR-3772', 'DK-18K-1XR-3774',
    'DK-18K-1XR-3775', 'DK-18K-1XR-3955', 'DK-18K-1XR-4004',
    'DK-18K-20A-26224', 'DK-18K-20A-26237', 'DK-18K-20A-26487'
)
ORDER BY stock_number;

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
    'SKU-S920028RD7YL1', 'SKU-BF309RDWL1', 'SKU-BF309SDWL2',
    'SKU-BF309YSDWL3', 'SKU-BB231205M34D7YL4', 'SKU-BB231205M1017YL2',
    'SKU-S920028M101D7YL2', 'SKU-S920028ED7YL1', 'SKU-S920028SD7WE1'
)
ORDER BY b.sku_code, b.location_code;

SELECT
    stock_number,
    SUM(qty) FILTER (WHERE movement_type IN ('RECEIPT', 'RETURN')) AS receipt_return_qty,
    COALESCE(SUM(qty) FILTER (WHERE movement_type = 'SALE'), 0) AS sale_qty,
    COALESCE(SUM(qty) FILTER (WHERE movement_type IN ('RECEIPT', 'RETURN')), 0)
        - COALESCE(SUM(qty) FILTER (WHERE movement_type = 'SALE'), 0) AS net_ledger_qty
FROM tbt_stock_movement
WHERE stock_number IN (
    'DK-18K-20A-27962', 'DK-18K-1XR-3772', 'DK-18K-1XR-3774',
    'DK-18K-1XR-3775', 'DK-18K-1XR-3955', 'DK-18K-1XR-4004',
    'DK-18K-20A-26224', 'DK-18K-20A-26237', 'DK-18K-20A-26487'
)
GROUP BY stock_number
ORDER BY stock_number;

-- =============================================
-- FIX — สำรองข้อมูลก่อน แล้วแก้ qty ผี 9 ชิ้น + balance 3 SKU ที่ขายจริงแล้ว
-- =============================================
BEGIN;

CREATE TABLE IF NOT EXISTS bak_20260915_phantom_gold_piece AS
SELECT * FROM tbt_stock_piece
WHERE stock_number IN (
    'DK-18K-20A-27962', 'DK-18K-1XR-3772', 'DK-18K-1XR-3774',
    'DK-18K-1XR-3775', 'DK-18K-1XR-3955', 'DK-18K-1XR-4004',
    'DK-18K-20A-26224', 'DK-18K-20A-26237', 'DK-18K-20A-26487'
);

CREATE TABLE IF NOT EXISTS bak_20260915_phantom_gold_balance AS
SELECT * FROM tbt_stock_balance
WHERE sku_code IN (
    'SKU-S920028RD7YL1', 'SKU-BF309RDWL1', 'SKU-BF309SDWL2',
    'SKU-BF309YSDWL3', 'SKU-BB231205M34D7YL4', 'SKU-BB231205M1017YL2',
    'SKU-S920028M101D7YL2', 'SKU-S920028ED7YL1', 'SKU-S920028SD7WE1'
);

-- กลุ่ม A — 6 ชิ้นที่ยังไม่เคยขายจริง qty ผี 5 -> ของจริง 1 ชิ้น คง IN_STOCK
UPDATE tbt_stock_piece
SET qty = 1,
    status = 'IN_STOCK',
    update_by = 'system-fix',
    update_date = now()
WHERE stock_number IN (
        'DK-18K-20A-27962', 'DK-18K-1XR-3772', 'DK-18K-1XR-3774',
        'DK-18K-1XR-3775', 'DK-18K-1XR-3955', 'DK-18K-1XR-4004'
    )
  AND location_code = 'MAIN'
  AND qty = 5
  AND qty_reserved = 0
  AND status = 'IN_STOCK';

-- กลุ่ม B — 3 ชิ้นที่ขายจริงไปแล้วคนละ 1 ของจริงหมดแล้ว qty ผี 4 -> 0 เปลี่ยนสถานะเป็น SOLD
UPDATE tbt_stock_piece
SET qty = 0,
    status = 'SOLD',
    update_by = 'system-fix',
    update_date = now()
WHERE stock_number IN ('DK-18K-20A-26224', 'DK-18K-20A-26237', 'DK-18K-20A-26487')
  AND location_code = 'STOCK01'
  AND qty = 4
  AND qty_reserved = 0
  AND status = 'IN_STOCK';

-- balance กลุ่ม B ที่ MAIN — ติดลบผี -5 -> 0
UPDATE tbt_stock_balance
SET qty_on_hand = 0,
    update_by = 'system-fix',
    update_date = now()
WHERE sku_code IN ('SKU-S920028M101D7YL2', 'SKU-S920028ED7YL1', 'SKU-S920028SD7WE1')
  AND location_code = 'MAIN'
  AND qty_on_hand = -5
  AND qty_reserved = 0;

-- balance กลุ่ม B ที่ STOCK01 — ค้างผี 5 -> 0
UPDATE tbt_stock_balance
SET qty_on_hand = 0,
    update_by = 'system-fix',
    update_date = now()
WHERE sku_code IN ('SKU-S920028M101D7YL2', 'SKU-S920028ED7YL1', 'SKU-S920028SD7WE1')
  AND location_code = 'STOCK01'
  AND qty_on_hand = 5
  AND qty_reserved = 0;

COMMIT;

-- =============================================
-- AFTER — รันซ้ำ query เดิมเพื่อเทียบผลก่อน/หลัง (ledger ต้องไม่เปลี่ยน) + ตรวจว่ายังผิดอยู่ไหม
-- =============================================
SELECT stock_number, sku_code, location_code, qty, qty_reserved, status, update_by, update_date
FROM tbt_stock_piece
WHERE stock_number IN (
    'DK-18K-20A-27962', 'DK-18K-1XR-3772', 'DK-18K-1XR-3774',
    'DK-18K-1XR-3775', 'DK-18K-1XR-3955', 'DK-18K-1XR-4004',
    'DK-18K-20A-26224', 'DK-18K-20A-26237', 'DK-18K-20A-26487'
)
ORDER BY stock_number;

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
    'SKU-S920028RD7YL1', 'SKU-BF309RDWL1', 'SKU-BF309SDWL2',
    'SKU-BF309YSDWL3', 'SKU-BB231205M34D7YL4', 'SKU-BB231205M1017YL2',
    'SKU-S920028M101D7YL2', 'SKU-S920028ED7YL1', 'SKU-S920028SD7WE1'
)
ORDER BY b.sku_code, b.location_code;

SELECT
    stock_number,
    SUM(qty) FILTER (WHERE movement_type IN ('RECEIPT', 'RETURN')) AS receipt_return_qty,
    COALESCE(SUM(qty) FILTER (WHERE movement_type = 'SALE'), 0) AS sale_qty,
    COALESCE(SUM(qty) FILTER (WHERE movement_type IN ('RECEIPT', 'RETURN')), 0)
        - COALESCE(SUM(qty) FILTER (WHERE movement_type = 'SALE'), 0) AS net_ledger_qty
FROM tbt_stock_movement
WHERE stock_number IN (
    'DK-18K-20A-27962', 'DK-18K-1XR-3772', 'DK-18K-1XR-3774',
    'DK-18K-1XR-3775', 'DK-18K-1XR-3955', 'DK-18K-1XR-4004',
    'DK-18K-20A-26224', 'DK-18K-20A-26237', 'DK-18K-20A-26487'
)
GROUP BY stock_number
ORDER BY stock_number;

-- ตรวจว่ายังผิดอยู่ไหม (a) — ต้องไม่มีแถวใดเลยถ้าแก้ถูกต้อง
SELECT stock_number, sku_code, location_code, qty, qty_reserved, status
FROM tbt_stock_piece
WHERE stock_number NOT LIKE 'DK-SIL-%'
  AND qty > 1;

-- ตรวจว่ายังผิดอยู่ไหม (b) — balance vs ผลรวม qty ของชิ้นที่ยังไม่ SOLD ของ 9 SKU
-- ต้องไม่มีแถวใดเลยถ้าแก้ถูกต้อง
SELECT
    b.sku_code,
    b.location_code,
    b.qty_on_hand,
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
    'SKU-S920028RD7YL1', 'SKU-BF309RDWL1', 'SKU-BF309SDWL2',
    'SKU-BF309YSDWL3', 'SKU-BB231205M34D7YL4', 'SKU-BB231205M1017YL2',
    'SKU-S920028M101D7YL2', 'SKU-S920028ED7YL1', 'SKU-S920028SD7WE1'
)
  AND b.qty_on_hand - COALESCE(p.piece_qty_sum, 0) <> 0;
