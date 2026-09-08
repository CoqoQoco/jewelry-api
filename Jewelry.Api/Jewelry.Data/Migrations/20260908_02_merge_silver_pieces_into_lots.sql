-- =============================================
-- Migration: Merge silver transfer pieces into lots (1 รหัสเก่า = 1 เลขใหม่ ถือ qty)
-- Date: 2026-09-08
-- Description: ก่อนรัน migration นี้ ตัวโอนเก่า (pre-fix) สร้าง 1 piece ต่อ 1 ชิ้นจริง
--              ทำให้ 1 stock_number_origin (รหัสเก่า) แตกเป็นหลาย stock_number (เลขใหม่)
--              migration นี้รวมกลับให้เหลือ 1 เลขใหม่ IN_STOCK ต่อ 1 รหัสเก่า โดยเก็บจำนวนไว้ที่ piece.qty
--              ดูรายละเอียด/ตัวเลขคาดหวังที่ docs/silver-lot-impact.md §5
--
-- Revision (2026-09-08): ขายเงินจริงเริ่มแล้วก่อนไฟล์นี้รัน (SIG04946 ล็อต 8 ชิ้น ขายไปแล้ว 2 ชิ้น
--              เช้าวันนี้บน INV260908013 / INV260908014 และมีแถวอ้างอิงใน tbt_sale_order_product)
--              เวอร์ชันเดิมของไฟล์นี้ pre-check ว่า "ห้ามมี piece ที่ไม่ใช่ IN_STOCK" ซึ่งจะ abort
--              migration ทุกวันที่มีการขายของเงินจริงเกิดขึ้นก่อน — และถ้าตัด pre-check นั้นทิ้งเฉย ๆ
--              โดยยังนับ lot_qty = COUNT(*) แบบเดิม จะดึงชิ้นที่ขายไปแล้วเข้ามานับในล็อตที่เหลือ ทำให้
--              ยอดพร้อมขายเฟ้อเกินจริง จึงออกแบบไฟล์นี้ใหม่ทั้งหมดให้ยึดหลัก:
--                * นับ/รวม/ย้าย/ลบ เฉพาะ piece ที่ status = 'IN_STOCK' เท่านั้น
--                * survivor ต้องเป็น IN_STOCK เสมอ แม้จะมี piece สถานะอื่นที่เลขต่ำกว่า/สั้นกว่าอยู่ในกลุ่ม
--                  (บังคับด้วย WHERE status = 'IN_STOCK' ตอนสร้าง _silver_lot_survivor เอง ไม่ใช่แค่ ORDER BY)
--                * ถ้าทั้งกลุ่ม (stock_number_origin เดียวกัน) ไม่เหลือ piece IN_STOCK แม้แต่ชิ้นเดียว
--                  (ขายหมดทั้งล็อตก่อนไฟล์นี้รัน) กลุ่มนั้นไม่ต้อง merge เลย — จะไม่มี survivor row เกิดขึ้น
--                  และทุก step ถัดไป (ย้าย movement, ลบ, UPDATE ขั้นตอนที่ 4, post-check) ข้ามกลุ่มนั้นไปเอง
--                  โดยอัตโนมัติ ไม่แตะ piece ไหนในกลุ่มนั้นแม้แต่ตัวเดียว
--                * piece ที่ไม่ใช่ IN_STOCK (เช่น 2 ชิ้นที่ขายไปแล้วของ SIG04946) ไม่ถูกแตะเลยไม่ว่าจุดใด
--                  ในไฟล์นี้ — row ของมันเอง, material, movement และการอ้างอิงใน sale order ยังอยู่ครบ
--                  100% เหมือนก่อนรัน migration (ยังมี qty = 0 จาก migration 01)
--
-- *** Run order: ต้องรันหลัง 20260908_01 (เพิ่มคอลัมน์ + backfill qty/qty_reserved ของแถว SOLD/RESERVED เดิม) ***
-- *** และหลัง deploy API + UI เวอร์ชันที่รู้จัก qty แล้วเท่านั้น (ดู docs/silver-lot-impact.md §6) ***
-- *** ก่อนรัน: แจ้งผู้ใช้หยุดขาย/จองสินค้าเงินชั่วคราว (กันไม่ให้มี piece กลายเป็น RESERVED
--     ระหว่าง merge กำลังรัน — RESERVED ทำให้ pre-check abort โดยตั้งใจ ต้องเคลียร์ด้วยมือก่อน) ***
-- ถ้ารันก่อน deploy โค้ดใหม่ — การขายล็อตครั้งแรกจะทำทั้งล็อตเป็น SOLD (โค้ดเก่าไม่รู้จัก qty) ห้ามสลับลำดับ
--
-- ขอบเขต: เฉพาะ piece ที่มาจากตัวโอนงานเงิน (receipt_number LIKE 'DKSIL%', stock_number ใหม่ขึ้นต้น 'DK-SIL-')
--         stock_silver (คิวระบบเก่า) ไม่แตะ · tbt_stock_balance ไม่เปลี่ยน (SUM(qty) ของ IN_STOCK ต้องเท่าเดิม)
--         piece ที่ไม่ใช่ IN_STOCK (SOLD/RESERVED) ไม่ถูกแตะไม่ว่าจุดใดในไฟล์นี้
--
-- ตัวเลขจาก dry-run 2026-09-08 (อ่านอย่างเดียว) — เป็นตัวเลข ณ วันนั้น ใช้เพื่ออ้างอิง/eyeball เท่านั้น
-- ไม่ใช่ค่าที่ assert ในสคริปต์ (จำนวนเปลี่ยนทุกวันที่มีคนขายของเงินจริง ดู post-check ด้านล่างซึ่งเป็น
-- invariant ที่คำนวณสด ไม่ใช่ตัวเลขตายตัว):
--   ล็อต IN_STOCK หลังรวม 2,346 · total qty (IN_STOCK) 12,939 · piece IN_STOCK ที่ถูกลบ 10,593
--   · piece SOLD ที่เก็บไว้ไม่แตะ 2 · กลุ่มที่เลขต่ำสุดดันเป็นของที่ขายไปแล้ว 0 · balance drift หลัง merge 0
--
-- FK ที่ต้องเคลียร์ก่อนลบ piece IN_STOCK ที่ไม่ใช่ survivor (composite stock_number, product_code):
--   tbt_stock_piece_material, tbt_stock_piece_cost_plan, tbt_stock_piece_cost_version, tbt_stock_movement
--   ลบ cost_plan ก่อน cost_version เสมอ (cost_plan.version_running -> cost_version.running)
--   piece ที่ไม่ใช่ IN_STOCK (SOLD ฯลฯ) ไม่อยู่ในรายการที่ต้องเคลียร์นี้ — แถวของมันคงเดิมทุกตาราง
-- =============================================

BEGIN;

-- =============================================
-- 0.A Pre-check ที่ไม่ต้องรู้ว่าใครเป็น survivor ก่อน — RAISE EXCEPTION แล้ว abort ทั้ง transaction
-- =============================================
DO $$
DECLARE
    bad_groups      INT;
    reserved_pieces INT;
BEGIN
    -- ทุก stock_number_origin ของชุดที่โอนเงินมา ต้องมี product_code/sku_code/location_code เดียวกันทั้งกลุ่ม
    -- (เงื่อนไขที่ทำให้ merge เป็น 1 piece ได้อย่างปลอดภัย)
    SELECT COUNT(*) INTO bad_groups
    FROM (
        SELECT stock_number_origin
        FROM tbt_stock_piece
        WHERE receipt_number LIKE 'DKSIL%'
          AND stock_number_origin IS NOT NULL
        GROUP BY stock_number_origin
        HAVING COUNT(DISTINCT product_code) > 1
            OR COUNT(DISTINCT sku_code) > 1
            OR COUNT(DISTINCT location_code) > 1
    ) mismatched;

    IF bad_groups > 0 THEN
        RAISE EXCEPTION 'Pre-check failed: % stock_number_origin group(s) have mismatched product_code/sku_code/location_code — abort merge', bad_groups;
    END IF;

    -- ห้ามมี silver piece สถานะ RESERVED ค้างอยู่ — การจองระหว่างทางต้องเคลียร์ด้วยมือก่อน merge
    -- ไม่งั้น qty_reserved ของ piece นั้นจะเพี้ยนเมื่อถูกดึงเข้า/ออกจากล็อต
    -- หมายเหตุ: piece ที่ SOLD ไม่ต้องเช็คตรงนี้ — ปล่อยผ่านและไม่ถูกแตะที่จุดใดของไฟล์นี้เลย
    SELECT COUNT(*) INTO reserved_pieces
    FROM tbt_stock_piece
    WHERE receipt_number LIKE 'DKSIL%'
      AND stock_number_origin IS NOT NULL
      AND status = 'RESERVED';

    IF reserved_pieces > 0 THEN
        RAISE EXCEPTION 'Pre-check failed: % silver piece(s) are RESERVED — settle the reservation by hand before merging', reserved_pieces;
    END IF;
END $$;

-- =============================================
-- 1. survivor ต่อรหัสเก่า = piece ที่ IN_STOCK และเป็นเลขต่ำสุดในกลุ่ม
--    ranked CTE กรอง WHERE status = 'IN_STOCK' ตั้งแต่แหล่งข้อมูล (ไม่ใช่แค่พึ่ง ORDER BY เหมือนเดิม)
--    จึงการันตีว่า survivor ที่ได้เป็น IN_STOCK เสมอ 100% — piece ที่ขายไปแล้ว/สถานะอื่นไม่มีทางถูกเลือก
--    เป็น survivor ไม่ว่าเลขของมันจะสั้นกว่า/ต่ำกว่าแค่ไหน เพราะไม่เข้าเซตนี้ตั้งแต่แรก (ORDER BY
--    (status <> 'IN_STOCK') ที่เหลืออยู่เป็นแค่ tie-breaker เผื่อไว้ ไม่ได้เป็นตัวการันตีหลักอีกต่อไป)
--    ผลข้างเคียงที่ตั้งใจ: ถ้ากลุ่มไหนไม่มี piece IN_STOCK เหลือเลย (ขายหมดล็อตแล้ว) ranked จะไม่มี row
--    ของกลุ่มนั้นเลย เท่ากับไม่มี survivor ให้กลุ่มนั้น — กลุ่มนั้นถูกข้ามทั้งกลุ่มโดยอัตโนมัติในทุก step
--    ถัดไป (ย้าย movement, ลบ non-survivor, UPDATE ขั้นตอนที่ 4, post-check invariant (a)/(b))
--    lot_qty = จำนวน piece ที่ IN_STOCK เท่านั้นในกลุ่ม (ไม่นับ SOLD/RESERVED — ตอนนี้เท่ากับ COUNT(*)
--    ของทุก row ใน ranked ของกลุ่มนั้นพอดี เพราะ ranked มีแต่ IN_STOCK แล้ว)
-- =============================================
CREATE TEMP TABLE _silver_lot_survivor ON COMMIT DROP AS
WITH ranked AS (
    SELECT
        stock_number_origin,
        stock_number,
        product_code,
        sku_code,
        location_code,
        status,
        COUNT(*) FILTER (WHERE status = 'IN_STOCK') OVER (PARTITION BY stock_number_origin) AS lot_qty,
        ROW_NUMBER() OVER (
            PARTITION BY stock_number_origin
            ORDER BY (status <> 'IN_STOCK'), length(stock_number), stock_number
        ) AS rn
    FROM tbt_stock_piece
    WHERE receipt_number LIKE 'DKSIL%'
      AND stock_number_origin IS NOT NULL
      AND status = 'IN_STOCK'
)
SELECT
    stock_number_origin,
    stock_number AS survivor_stock_number,
    product_code AS survivor_product_code,
    sku_code AS survivor_sku_code,
    location_code AS survivor_location_code,
    status AS survivor_status,
    lot_qty
FROM ranked
WHERE rn = 1;

-- piece ที่ IN_STOCK แต่ไม่ใช่ survivor ของกลุ่มตัวเอง (ตัวที่ต้องรวม/ลบทิ้งหลัง merge)
-- ตั้งใจกรอง p.status = 'IN_STOCK' ตรงนี้ — piece ที่ SOLD/RESERVED จะไม่มีทางเข้าเซตนี้เลย
-- แม้จะไม่ใช่ survivor ก็ตาม (piece ที่ไม่ใช่ IN_STOCK ต้องไม่ถูกแตะไม่ว่ากรณีใด)
CREATE TEMP TABLE _silver_lot_nonsurvivor ON COMMIT DROP AS
SELECT p.stock_number, p.product_code, s.survivor_stock_number, s.survivor_product_code, s.lot_qty
FROM tbt_stock_piece p
JOIN _silver_lot_survivor s ON s.stock_number_origin = p.stock_number_origin
WHERE p.receipt_number LIKE 'DKSIL%'
  AND p.stock_number_origin IS NOT NULL
  AND p.status = 'IN_STOCK'
  AND NOT (p.stock_number = s.survivor_stock_number AND p.product_code = s.survivor_product_code);

-- =============================================
-- 0.B Pre-check ที่ต้องรู้ non-survivor ก่อนถึงเช็คได้ — RAISE EXCEPTION แล้ว abort ทั้ง transaction
--     อ้างอิงไป survivor หรือไป piece ที่ SOLD แล้ว (เก็บไว้เฉย ๆ) ผ่านได้ปกติ — เฉพาะอ้างอิงไป
--     IN_STOCK non-survivor ที่กำลังจะถูกลบเท่านั้นที่ต้อง abort
-- =============================================
DO $$
DECLARE
    so_refs       INT;
    basket_refs   INT;
    shipment_refs INT;
BEGIN
    SELECT COUNT(*) INTO so_refs
    FROM tbt_sale_order_product sop
    JOIN _silver_lot_nonsurvivor ns ON ns.stock_number = sop.stock_number;

    SELECT COUNT(*) INTO basket_refs
    FROM tbt_stock_basket_item bi
    JOIN _silver_lot_nonsurvivor ns ON ns.stock_number = bi.stock_number;

    SELECT COUNT(*) INTO shipment_refs
    FROM tbt_export_shipment_item esi
    JOIN _silver_lot_nonsurvivor ns ON ns.stock_number = esi.stock_number;

    IF so_refs + basket_refs + shipment_refs > 0 THEN
        RAISE EXCEPTION 'Pre-check failed: % tbt_sale_order_product, % tbt_stock_basket_item, % tbt_export_shipment_item row(s) reference an IN_STOCK piece that would be deleted by the merge — abort', so_refs, basket_refs, shipment_refs;
    END IF;
END $$;

-- =============================================
-- 2. ย้าย movement ที่ไม่ใช่ RECEIPT ของ IN_STOCK non-survivor -> survivor (คาด 0 แถว ตาม dry-run)
-- =============================================
UPDATE tbt_stock_movement mv
SET stock_number = ns.survivor_stock_number,
    product_code = ns.survivor_product_code
FROM _silver_lot_nonsurvivor ns
WHERE mv.stock_number = ns.stock_number
  AND mv.product_code = ns.product_code
  AND mv.movement_type <> 'RECEIPT';

-- =============================================
-- 3. ลบ material / cost_plan / cost_version / movement RECEIPT ของ IN_STOCK non-survivor เท่านั้น
--    (ลบ cost_plan ก่อน cost_version เพราะ cost_plan.version_running -> cost_version.running)
--    piece ที่ SOLD/RESERVED ไม่อยู่ใน _silver_lot_nonsurvivor จึงไม่มีทางถูกลบตรงนี้
-- =============================================
DELETE FROM tbt_stock_piece_material m
USING _silver_lot_nonsurvivor ns
WHERE m.stock_number = ns.stock_number
  AND m.product_code = ns.product_code;

DELETE FROM tbt_stock_piece_cost_plan cp
USING _silver_lot_nonsurvivor ns
WHERE cp.stock_number = ns.stock_number
  AND cp.product_code = ns.product_code;

DELETE FROM tbt_stock_piece_cost_version cv
USING _silver_lot_nonsurvivor ns
WHERE cv.stock_number = ns.stock_number
  AND cv.product_code = ns.product_code;

DELETE FROM tbt_stock_movement mv
USING _silver_lot_nonsurvivor ns
WHERE mv.stock_number = ns.stock_number
  AND mv.product_code = ns.product_code
  AND mv.movement_type = 'RECEIPT';

-- =============================================
-- 4. survivor: qty = จำนวน piece IN_STOCK ในล็อต, qty_reserved = 0, status IN_STOCK
--    + movement RECEIPT ของ survivor ปรับ qty เท่ากัน
--    (survivor การันตีว่าเป็น IN_STOCK เสมอ เพราะ _silver_lot_survivor ขั้นตอนที่ 1 กรอง
--    WHERE status = 'IN_STOCK' ไว้ตั้งแต่ตอนสร้างตารางแล้ว ไม่ใช่แค่จาก ORDER BY — SET status ตรงนี้
--    จึงเป็น no-op ที่ปลอดภัยจริง ๆ)
--    กลุ่มที่ขายหมดทั้งล็อต (ไม่มี piece IN_STOCK เหลือเลย) จะไม่มี row ใน _silver_lot_survivor เลย
--    UPDATE นี้จึงไม่ match แถวไหนของกลุ่มนั้น — ถูกข้ามทั้งกลุ่มโดยตั้งใจ ไม่มี piece ไหนถูกแตะ
-- =============================================
UPDATE tbt_stock_piece p
SET qty = s.lot_qty,
    qty_reserved = 0,
    status = 'IN_STOCK',
    update_date = NOW(),
    update_by = 'MIGRATION_SILVER_LOT'
FROM _silver_lot_survivor s
WHERE p.stock_number = s.survivor_stock_number
  AND p.product_code = s.survivor_product_code;

UPDATE tbt_stock_movement mv
SET qty = s.lot_qty
FROM _silver_lot_survivor s
WHERE mv.stock_number = s.survivor_stock_number
  AND mv.product_code = s.survivor_product_code
  AND mv.movement_type = 'RECEIPT';

-- =============================================
-- 5. ลบ piece IN_STOCK ที่ไม่ใช่ survivor (หลังลูกทุกตัวถูกลบ/ย้ายแล้ว)
--    piece ที่ SOLD/RESERVED ไม่อยู่ใน _silver_lot_nonsurvivor จึงไม่ถูกลบ — คงอยู่ครบ 100%
--    (row เอง, material, movement, การอ้างอิงใน sale order ไม่ถูกแตะเลย)
-- =============================================
DELETE FROM tbt_stock_piece p
USING _silver_lot_nonsurvivor ns
WHERE p.stock_number = ns.stock_number
  AND p.product_code = ns.product_code;

-- =============================================
-- 6. Post-check — invariant ที่ต้องจริงเสมอไม่ว่าจะรันวันไหน (ไม่ hardcode ตัวเลขของวันที่ dry-run)
--    RAISE NOTICE ไว้ให้ operator eyeball ตัวเลขจริงก่อน, RAISE EXCEPTION เฉพาะเมื่อ invariant พัง
-- =============================================
DO $$
DECLARE
    lot_count      INT;
    total_qty      NUMERIC;
    deleted_count  INT;
    sold_kept      INT;
    skipped_groups INT;
    bad_lot_groups INT;
    drift_rows     INT;
    orphan_rows    INT;
BEGIN
    -- ตัวเลขไว้ให้ operator eyeball เทียบกับ dry-run (ไม่ assert)
    SELECT COUNT(*) INTO lot_count
    FROM tbt_stock_piece
    WHERE receipt_number LIKE 'DKSIL%'
      AND stock_number_origin IS NOT NULL
      AND status = 'IN_STOCK';

    SELECT COALESCE(SUM(qty), 0) INTO total_qty
    FROM tbt_stock_piece
    WHERE receipt_number LIKE 'DKSIL%'
      AND stock_number_origin IS NOT NULL
      AND status = 'IN_STOCK';

    SELECT COUNT(*) INTO deleted_count FROM _silver_lot_nonsurvivor;

    SELECT COUNT(*) INTO sold_kept
    FROM tbt_stock_piece
    WHERE receipt_number LIKE 'DKSIL%'
      AND stock_number_origin IS NOT NULL
      AND status <> 'IN_STOCK';

    -- กลุ่มที่ขายหมดทั้งล็อตแล้ว (ไม่มี piece IN_STOCK เหลือเลย) ไม่มี survivor row ตั้งแต่ขั้นตอนที่ 1
    -- จึงไม่ถูก merge เลยตามการออกแบบ (ไม่ใช่ error) — นับไว้ให้ operator เห็นว่าเกิดขึ้นกี่กลุ่ม
    SELECT COUNT(*) INTO skipped_groups
    FROM (
        SELECT stock_number_origin
        FROM tbt_stock_piece
        WHERE receipt_number LIKE 'DKSIL%'
          AND stock_number_origin IS NOT NULL
        GROUP BY stock_number_origin
        HAVING COUNT(*) FILTER (WHERE status = 'IN_STOCK') = 0
    ) fully_sold;

    RAISE NOTICE 'Silver lot merge result: % surviving IN_STOCK lot(s), total qty %, % IN_STOCK piece(s) deleted, % non-IN_STOCK piece(s) left untouched, % fully-sold origin group(s) skipped (no IN_STOCK piece left, not merged by design)', lot_count, total_qty, deleted_count, sold_kept, skipped_groups;

    -- Invariant (a): ทุก stock_number_origin ที่ "มี survivor" (คือมี piece IN_STOCK เหลืออย่างน้อย 1
    -- ชิ้นก่อน merge) ต้องเหลือ piece IN_STOCK เดียวเป๊ะหลัง merge — survivor ถูกกรองให้เป็น IN_STOCK
    -- เสมอตั้งแต่ขั้นตอนที่ 1 จึงไม่มีทางเป็นค่าอื่น กลุ่มที่ขายหมดทั้งล็อต (ไม่มี piece IN_STOCK เหลือ
    -- เลย) จะไม่มี survivor row ให้ loop นี้ไปตรวจตั้งแต่แรก — ถูกข้ามโดยตั้งใจ ไม่ใช่ช่องโหว่ (ดูจำนวน
    -- กลุ่มที่ถูกข้ามใน RAISE NOTICE ด้านบน)
    SELECT COUNT(*) INTO bad_lot_groups
    FROM _silver_lot_survivor s
    WHERE (
        SELECT COUNT(*) FROM tbt_stock_piece p
        WHERE p.stock_number_origin = s.stock_number_origin
          AND p.status = 'IN_STOCK'
    ) <> 1;

    IF bad_lot_groups > 0 THEN
        RAISE EXCEPTION 'Post-check failed: % stock_number_origin group(s) do not have exactly one IN_STOCK piece after merge', bad_lot_groups;
    END IF;

    -- Invariant (b): SUM(piece.qty) ต่อ (sku_code, location_code) ของทุกล็อตที่ merge นี้แตะ
    --                ต้องเท่ากับ tbt_stock_balance.qty_on_hand (balance ไม่ถูกแก้โดยไฟล์นี้เลย)
    WITH touched AS (
        SELECT DISTINCT survivor_sku_code AS sku_code, survivor_location_code AS location_code
        FROM _silver_lot_survivor
    )
    SELECT COUNT(*) INTO drift_rows
    FROM touched t
    JOIN tbt_stock_balance b
      ON b.sku_code = t.sku_code
     AND b.location_code = t.location_code
    WHERE (
        SELECT COALESCE(SUM(p.qty), 0)
        FROM tbt_stock_piece p
        WHERE p.sku_code = t.sku_code
          AND p.location_code = t.location_code
    ) <> b.qty_on_hand;

    IF drift_rows > 0 THEN
        RAISE EXCEPTION 'Post-check failed: % (sku, location) group(s) touched by the merge have piece.qty sum <> balance.qty_on_hand', drift_rows;
    END IF;

    -- Invariant (c): ห้ามมี material/movement/cost row ค้างอ้าง stock_number ที่ถูกลบไปแล้ว
    SELECT COUNT(*) INTO orphan_rows
    FROM _silver_lot_nonsurvivor ns
    WHERE EXISTS (SELECT 1 FROM tbt_stock_piece_material m WHERE m.stock_number = ns.stock_number AND m.product_code = ns.product_code)
       OR EXISTS (SELECT 1 FROM tbt_stock_movement mv WHERE mv.stock_number = ns.stock_number AND mv.product_code = ns.product_code)
       OR EXISTS (SELECT 1 FROM tbt_stock_piece_cost_plan cp WHERE cp.stock_number = ns.stock_number AND cp.product_code = ns.product_code)
       OR EXISTS (SELECT 1 FROM tbt_stock_piece_cost_version cv WHERE cv.stock_number = ns.stock_number AND cv.product_code = ns.product_code);

    IF orphan_rows > 0 THEN
        RAISE EXCEPTION 'Post-check failed: % deleted stock_number(s) still have orphaned material/movement/cost row(s)', orphan_rows;
    END IF;
END $$;

COMMIT;
