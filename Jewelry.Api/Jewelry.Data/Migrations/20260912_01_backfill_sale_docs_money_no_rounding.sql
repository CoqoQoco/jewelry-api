-- =============================================
-- Migration: ปรับยอดเงินเอกสารขายเก่าให้ตรงมาตรฐานใหม่ (ตรงเครื่องคิดเลข)
-- Date: 2026-09-12
-- Description:
--   มาตรฐานใหม่ของเอกสารขายทั้งระบบ: ไม่ปัดเศษระหว่างทางเลย ปัดครั้งเดียวที่ยอดสุดท้าย
--   เป็นทศนิยม 2 ตำแหน่ง ใช้เกณฑ์เดียวกันทั้งใบเสนอราคา ใบสั่งขาย ใบแจ้งหนี้ ทุกสกุลเงิน
--
--   สคริปต์นี้แก้ข้อมูลที่บันทึกไปแล้วให้ตรงมาตรฐานใหม่ 2 ส่วน
--     ส่วนที่ 1  เอกสารที่ถูกปัด "ราคาต่อชิ้น" ตอนบันทึก (ช่วง 11-12 ก.ย. 2026)
--                คำนวณ sub_total ใหม่จากรายการสินค้าจริง — มี 2 ใบ คือ INV260912003 กับ SO260910014
--     ส่วนที่ 2  ทุกเอกสารที่ยอดสุดท้ายถูกปัดขึ้นเป็นจำนวนเต็ม
--                คืนเป็นยอดจริงทศนิยม 2 ตำแหน่ง และล้าง rounding_adjustment เป็น 0
--
--   ขอบเขตที่คาดไว้ (ณ วันเขียนสคริปต์)
--     ใบแจ้งหนี้   39 ใบจาก 119
--     ใบสั่งขาย    30 ใบจาก 112
--     ใบเสนอราคา   24 ใบจาก  83
--     เศษที่ถูกปัดขึ้นรวมทั้งหมด 18.10 US$
--
--   !! ใบที่รับชำระเงินไปแล้วตามยอดที่ปัด มี 3 ใบ หลังรันจะกลายเป็นชำระเกิน รวม 1.06 US$
--        INV260908001  61.00 -> 60.55    เกิน 0.45
--        INV260910001  2,867 -> 2,866.77 เกิน 0.23
--        INV260910010  1,265 -> 1,264.62 เกิน 0.38
--      user ยืนยันให้แก้ทั้งหมดแล้ว เพราะ user และลูกค้าเอาตัวเลขไปคีย์ Excel เอง
--      ยอดในระบบจึงต้องเป็นยอดจริงเสมอ ถ้าต้องการปิดยอดชำระเกินให้ไปปรับที่รายการรับชำระแยกต่างหาก
--
--   !! สคริปต์นี้แก้ข้อมูลจริงบน production — ให้ user รันเอง ห้าม agent รัน
--      รัน "ส่วน DRY-RUN" ข้างล่างก่อนเสมอ เพื่อดูรายการที่จะโดนแก้
-- =============================================


-- =============================================
-- DRY-RUN: รันชุดนี้ก่อน เพื่อดูว่าจะมีอะไรเปลี่ยนบ้าง (ไม่แก้ข้อมูล)
-- =============================================
-- SELECT 'invoice' AS doc_type, running AS doc, currency_unit,
--        grand_total_rounded AS before_total, ROUND(grand_total_raw, 2) AS after_total
--   FROM tbt_sale_invoice_header
--  WHERE grand_total_raw IS NOT NULL
--    AND grand_total_rounded IS DISTINCT FROM ROUND(grand_total_raw, 2)
-- UNION ALL
-- SELECT 'sale_order', so_number, currency_unit,
--        grand_total_rounded, ROUND(grand_total_raw, 2)
--   FROM tbt_sale_order
--  WHERE grand_total_raw IS NOT NULL
--    AND grand_total_rounded IS DISTINCT FROM ROUND(grand_total_raw, 2)
-- UNION ALL
-- SELECT 'quotation', number, currency,
--        grand_total_rounded, ROUND(grand_total_raw, 2)
--   FROM tbt_sale_quotation
--  WHERE grand_total_raw IS NOT NULL
--    AND grand_total_rounded IS DISTINCT FROM ROUND(grand_total_raw, 2)
--  ORDER BY 1, 2;


BEGIN;

-- =============================================
-- 0. สำรองยอดเงินเดิมไว้ก่อน (กู้คืนได้ถ้าผลไม่ถูกต้อง)
-- =============================================
CREATE TABLE IF NOT EXISTS bak_20260912_invoice_money AS
SELECT running, sub_total, vat_amount, grand_total_raw, grand_total_rounded, rounding_adjustment
  FROM tbt_sale_invoice_header;

CREATE TABLE IF NOT EXISTS bak_20260912_sale_order_money AS
SELECT so_number, sub_total, vat_amount, grand_total_raw, grand_total_rounded, rounding_adjustment
  FROM tbt_sale_order;

CREATE TABLE IF NOT EXISTS bak_20260912_quotation_money AS
SELECT number, sub_total, vat_amount, grand_total_raw, grand_total_rounded, rounding_adjustment
  FROM tbt_sale_quotation;


-- =============================================
-- 1. เอกสารที่ถูกปัดราคาต่อชิ้นตอนบันทึก — คิด sub_total ใหม่จากรายการสินค้าจริง
--    เจาะจงเลขเอกสารไว้ เพราะเอกสารอื่นที่ยอดไม่ตรงเกิดจากสินค้าคนละชุด ไม่ใช่เรื่องปัดเศษ
-- =============================================

-- 1.1 ใบแจ้งหนี้ INV260912003 : 9,689 -> 9,689.60
WITH calc AS (
    SELECT h.running,
           ROUND(SUM(p.price_origin * (1 - COALESCE(p.discount, 0) / 100)
                     / NULLIF(h.currency_rate, 0) * p.qty), 2) AS sub_total_new
      FROM tbt_sale_invoice_header h
      JOIN tbt_sale_order_product p ON p.invoice = h.running
     WHERE h.running = 'INV260912003'
     GROUP BY h.running
)
UPDATE tbt_sale_invoice_header h
   SET sub_total       = c.sub_total_new,
       vat_amount      = ROUND((c.sub_total_new
                                - COALESCE(h.special_discount_amt, 0)
                                + COALESCE(h.special_addition_amt, 0)
                                + COALESCE(h.freight_amt, 0)) * COALESCE(h.vat, 0) / 100, 2),
       grand_total_raw = ROUND((c.sub_total_new
                                - COALESCE(h.special_discount_amt, 0)
                                + COALESCE(h.special_addition_amt, 0)
                                + COALESCE(h.freight_amt, 0)) * (1 + COALESCE(h.vat, 0) / 100), 2)
  FROM calc c
 WHERE h.running = c.running;

-- 1.2 ใบสั่งขาย SO260910014 : 9,689 -> 9,689.60
WITH calc AS (
    SELECT s.so_number,
           ROUND(SUM(p.price_origin * (1 - COALESCE(p.discount, 0) / 100)
                     / NULLIF(s.currency_rate, 0) * p.qty), 2) AS sub_total_new
      FROM tbt_sale_order s
      JOIN tbt_sale_order_product p ON p.so_number = s.so_number
     WHERE s.so_number = 'SO260910014'
     GROUP BY s.so_number
)
UPDATE tbt_sale_order s
   SET sub_total       = c.sub_total_new,
       vat_amount      = ROUND((c.sub_total_new
                                - COALESCE(s.special_discount_amt, 0)
                                + COALESCE(s.special_addition_amt, 0)
                                + COALESCE(s.freight_amt, 0)) * COALESCE(s.vat, 0) / 100, 2),
       grand_total_raw = ROUND((c.sub_total_new
                                - COALESCE(s.special_discount_amt, 0)
                                + COALESCE(s.special_addition_amt, 0)
                                + COALESCE(s.freight_amt, 0)) * (1 + COALESCE(s.vat, 0) / 100), 2)
  FROM calc c
 WHERE s.so_number = c.so_number;


-- =============================================
-- 2. ทุกเอกสาร — ยอดสุดท้ายเป็นยอดจริงทศนิยม 2 ตำแหน่ง และล้างเศษปัดทิ้ง
--    (ปัดเป็น 2 ตำแหน่งด้วยเพื่อล้าง noise ของ floating point เช่น 77312.400000000000000000000004)
-- =============================================

UPDATE tbt_sale_invoice_header
   SET sub_total           = ROUND(sub_total, 2),
       vat_amount          = ROUND(COALESCE(vat_amount, 0), 2),
       grand_total_raw     = ROUND(grand_total_raw, 2),
       grand_total_rounded = ROUND(grand_total_raw, 2),
       rounding_adjustment = 0
 WHERE grand_total_raw IS NOT NULL
   AND (grand_total_rounded IS DISTINCT FROM ROUND(grand_total_raw, 2)
        OR COALESCE(rounding_adjustment, 0) <> 0
        OR sub_total IS DISTINCT FROM ROUND(sub_total, 2)
        OR vat_amount IS DISTINCT FROM ROUND(COALESCE(vat_amount, 0), 2));

UPDATE tbt_sale_order
   SET sub_total           = ROUND(sub_total, 2),
       vat_amount          = ROUND(COALESCE(vat_amount, 0), 2),
       grand_total_raw     = ROUND(grand_total_raw, 2),
       grand_total_rounded = ROUND(grand_total_raw, 2),
       rounding_adjustment = 0
 WHERE grand_total_raw IS NOT NULL
   AND (grand_total_rounded IS DISTINCT FROM ROUND(grand_total_raw, 2)
        OR COALESCE(rounding_adjustment, 0) <> 0
        OR sub_total IS DISTINCT FROM ROUND(sub_total, 2)
        OR vat_amount IS DISTINCT FROM ROUND(COALESCE(vat_amount, 0), 2));

UPDATE tbt_sale_quotation
   SET sub_total           = ROUND(sub_total, 2),
       vat_amount          = ROUND(COALESCE(vat_amount, 0), 2),
       grand_total_raw     = ROUND(grand_total_raw, 2),
       grand_total_rounded = ROUND(grand_total_raw, 2),
       rounding_adjustment = 0
 WHERE grand_total_raw IS NOT NULL
   AND (grand_total_rounded IS DISTINCT FROM ROUND(grand_total_raw, 2)
        OR COALESCE(rounding_adjustment, 0) <> 0
        OR sub_total IS DISTINCT FROM ROUND(sub_total, 2)
        OR vat_amount IS DISTINCT FROM ROUND(COALESCE(vat_amount, 0), 2));


-- =============================================
-- 3. ตรวจผล — ทุกบรรทัดต้องได้ 0 ทั้งหมด
-- =============================================
SELECT 'invoice'    AS doc_type,
       COUNT(*) FILTER (WHERE COALESCE(rounding_adjustment, 0) <> 0)                      AS still_has_rounding,
       COUNT(*) FILTER (WHERE grand_total_raw IS NOT NULL
                          AND grand_total_rounded IS DISTINCT FROM ROUND(grand_total_raw, 2)) AS total_mismatch
  FROM tbt_sale_invoice_header
UNION ALL
SELECT 'sale_order',
       COUNT(*) FILTER (WHERE COALESCE(rounding_adjustment, 0) <> 0),
       COUNT(*) FILTER (WHERE grand_total_raw IS NOT NULL
                          AND grand_total_rounded IS DISTINCT FROM ROUND(grand_total_raw, 2))
  FROM tbt_sale_order
UNION ALL
SELECT 'quotation',
       COUNT(*) FILTER (WHERE COALESCE(rounding_adjustment, 0) <> 0),
       COUNT(*) FILTER (WHERE grand_total_raw IS NOT NULL
                          AND grand_total_rounded IS DISTINCT FROM ROUND(grand_total_raw, 2))
  FROM tbt_sale_quotation;

-- ใบตัวอย่างที่ลูกค้าทักมา ต้องได้ 77312.40 ทั้ง 3 ช่อง
SELECT running, currency_unit, sub_total, grand_total_raw, grand_total_rounded, rounding_adjustment
  FROM tbt_sale_invoice_header
 WHERE running IN ('INV260911003', 'INV260912003');

COMMIT;


-- =============================================
-- วิธีกู้คืนถ้าผลไม่ถูกต้อง
-- =============================================
-- UPDATE tbt_sale_invoice_header h SET sub_total = b.sub_total, vat_amount = b.vat_amount,
--        grand_total_raw = b.grand_total_raw, grand_total_rounded = b.grand_total_rounded,
--        rounding_adjustment = b.rounding_adjustment
--   FROM bak_20260912_invoice_money b WHERE b.running = h.running;
--
-- UPDATE tbt_sale_order s SET sub_total = b.sub_total, vat_amount = b.vat_amount,
--        grand_total_raw = b.grand_total_raw, grand_total_rounded = b.grand_total_rounded,
--        rounding_adjustment = b.rounding_adjustment
--   FROM bak_20260912_sale_order_money b WHERE b.so_number = s.so_number;
--
-- UPDATE tbt_sale_quotation q SET sub_total = b.sub_total, vat_amount = b.vat_amount,
--        grand_total_raw = b.grand_total_raw, grand_total_rounded = b.grand_total_rounded,
--        rounding_adjustment = b.rounding_adjustment
--   FROM bak_20260912_quotation_money b WHERE b.number = q.number;
