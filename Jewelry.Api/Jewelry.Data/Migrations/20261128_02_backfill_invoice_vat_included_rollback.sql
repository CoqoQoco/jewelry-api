-- Rollback: 20261128_02_backfill_invoice_vat_included.sql
-- ลำดับ: รันไฟล์นี้ก่อน แล้วค่อย 20261128_01_..._rollback.sql (ลบคอลัมน์ vat_included)
-- ถ้า backfill 02 ไม่เคยรัน ตาราง bak_20261128_invoice_money จะไม่มี — ให้รันเฉพาะส่วน "ขั้น 2" (ข้ามขั้น 1 ทั้งก้อน)
-- ขั้น 1) กู้ใบที่สำรองไว้จาก bak_20261128_invoice_money
-- ขั้น 2) ใบที่ vat_included = true แต่ไม่อยู่ใน bak (สร้างหลัง deploy) คืนเป็นสูตรเดิม (ยอดตามป้าย ไม่ถอด VAT)
BEGIN;
-- ===== ขั้น 1: กู้จาก bak (ข้ามถ้าไม่มีตาราง bak) =====
UPDATE tbt_sale_invoice_header h
   SET sub_total            = b.sub_total,
       special_discount_amt = b.special_discount_amt,
       special_addition_amt = b.special_addition_amt,
       freight_amt          = b.freight_amt,
       vat_amount           = b.vat_amount,
       grand_total_raw      = b.grand_total_raw,
       grand_total_rounded  = b.grand_total_rounded,
       rounding_adjustment  = b.rounding_adjustment,
       vat_included         = b.vat_included
  FROM bak_20261128_invoice_money b
 WHERE b.running = h.running;

-- ===== ขั้น 2: ไม่อ้าง bak — หลังขั้น 1 ใบที่กู้แล้ว vat_included = false จึงไม่ถูกแตะ =====
WITH subs AS (
    SELECT p.invoice AS running,
           SUM(p.price_origin * (1 - COALESCE(p.discount, 0) / 100.0) * p.qty) AS sub
      FROM tbt_sale_order_product p
     WHERE p.invoice IN (SELECT running FROM tbt_sale_invoice_header WHERE vat_included = true)
     GROUP BY p.invoice
)
UPDATE tbt_sale_invoice_header h
   SET sub_total            = s.sub,
       special_discount_amt = h.special_discount,
       special_addition_amt = h.special_addition,
       freight_amt          = h.freight_and_insurance,
       vat_amount           = 0,
       grand_total_raw      = s.sub - h.special_discount + h.special_addition + h.freight_and_insurance,
       grand_total_rounded  = round(s.sub - h.special_discount + h.special_addition + h.freight_and_insurance, 2),
       rounding_adjustment  = 0,
       vat_included         = false
  FROM subs s
 WHERE s.running = h.running;
COMMIT;
-- เก็บตาราง bak_20261128_invoice_money ไว้จนมั่นใจ แล้วค่อย DROP TABLE
