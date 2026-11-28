-- =============================================
-- Backfill: invoice THB ที่ vat=0 ให้เป็นราคารวม VAT (vat_included) TK202611280005
-- Date: 2026-11-28
-- ถ้ามีตาราง bak_20261128_invoice_money ค้างอยู่ สคริปต์จะ error ตั้งใจ (กันทับสำรองเดิม)
-- ลำดับ rollback: 20261128_02_rollback ก่อน แล้วค่อย 20261128_01_rollback
-- ลำดับ deploy: 20261128_01 -> API + UI -> สคริปต์นี้เป็นขั้นสุดท้าย
--
-- กติกา (ต้องตรง MathHelper.ComputeInvoiceTotalsVatIncluded / money.js)
--   round2(x)  = round(round(x, 6), 2)   (numeric ของ Postgres ปัดครึ่งออกจากศูนย์)
--   unitEx     = round2(price_origin * (1 - discount/100) / 1.07)
--   row        = round2(unitEx * qty)
--   sub        = sum(row)
--   *_amt      = round2(special / 1.07)   ; special_* ในตารางคงเป็นค่าที่กรอก (รวม VAT)
--   after      = sub - discEx + addEx + freightEx
--   vat        = round2(after * 0.07) ; grand = after + vat
--   vat ของ header คงเป็น 0 ; vat_included = true
--
-- ขอบเขต: THB, vat=0, currency_rate=1, ไม่ลบ, ไม่ใช่ MATERIAL, ไม่ใช่ POS (tbt_pos_checkout.invoice_number)
-- ยกเว้น: ใบที่รับเงินครบแล้ว (grand - deposit - sum(payment) <= 0.005) และ INV260907006
--
-- !! แก้ข้อมูลจริงบน production — ให้ user รันเอง ห้าม agent รัน · รัน DRY-RUN ก่อนเสมอ
-- =============================================

-- DRY-RUN (ไม่แก้ข้อมูล): ดูรายการ before/after
-- WITH target AS (
--   SELECT h.running FROM tbt_sale_invoice_header h
--    WHERE h.is_delete = false AND h.vat_included = false
--      AND upper(trim(h.currency_unit)) = 'THB' AND h.currency_rate = 1 AND COALESCE(h.vat, 0) = 0
--      AND COALESCE(h.invoice_type, 'PRODUCT') <> 'MATERIAL'
--      AND h.running <> 'INV260907006'
--      AND NOT EXISTS (SELECT 1 FROM tbt_pos_checkout pc WHERE pc.invoice_number = h.running)
--      AND (COALESCE(h.grand_total_rounded, 0) - COALESCE(h.deposit, 0)
--           - COALESCE((SELECT SUM(p.amount) FROM tbt_sale_invoice_payment_item p
--                        WHERE p.invoice_running = h.running AND p.is_delete = false), 0)) > 0.005
-- )
-- SELECT h.running, h.grand_total_rounded FROM tbt_sale_invoice_header h JOIN target t USING (running);

BEGIN;

-- 0. สำรองยอดเดิมของใบที่จะแก้
CREATE TABLE bak_20261128_invoice_money AS
SELECT h.running, h.sub_total, h.special_discount_amt, h.special_addition_amt, h.freight_amt,
       h.vat_amount, h.grand_total_raw, h.grand_total_rounded, h.rounding_adjustment, h.vat_included
  FROM tbt_sale_invoice_header h
 WHERE h.is_delete = false AND h.vat_included = false
   AND upper(trim(h.currency_unit)) = 'THB' AND h.currency_rate = 1 AND COALESCE(h.vat, 0) = 0
   AND COALESCE(h.invoice_type, 'PRODUCT') <> 'MATERIAL'
   AND h.running <> 'INV260907006'
   AND NOT EXISTS (SELECT 1 FROM tbt_pos_checkout pc WHERE pc.invoice_number = h.running)
   AND (COALESCE(h.grand_total_rounded, 0) - COALESCE(h.deposit, 0)
        - COALESCE((SELECT SUM(p.amount) FROM tbt_sale_invoice_payment_item p
                     WHERE p.invoice_running = h.running AND p.is_delete = false), 0)) > 0.005;

-- 1. คำนวณใหม่
WITH rows AS (
    SELECT p.invoice AS running,
           round(round(round(round(p.price_origin * (1 - COALESCE(p.discount, 0) / 100.0) / 1.07, 6), 2) * p.qty, 6), 2) AS row_ex
      FROM tbt_sale_order_product p
     WHERE p.invoice IN (SELECT running FROM bak_20261128_invoice_money)
),
subs AS (
    SELECT running, SUM(row_ex) AS sub FROM rows GROUP BY running
),
calc AS (
    SELECT h.running, s.sub,
           round(round(h.special_discount / 1.07, 6), 2)       AS disc_ex,
           round(round(h.special_addition / 1.07, 6), 2)       AS add_ex,
           round(round(h.freight_and_insurance / 1.07, 6), 2)  AS freight_ex
      FROM tbt_sale_invoice_header h
      JOIN subs s ON s.running = h.running
),
fin AS (
    SELECT c.*, (c.sub - c.disc_ex + c.add_ex + c.freight_ex) AS after FROM calc c
),
fin2 AS (
    SELECT f.*, round(round(f.after * 0.07, 6), 2) AS vat_amt FROM fin f
)
UPDATE tbt_sale_invoice_header h
   SET sub_total            = f.sub,
       special_discount_amt = f.disc_ex,
       special_addition_amt = f.add_ex,
       freight_amt          = f.freight_ex,
       vat_amount           = f.vat_amt,
       grand_total_raw      = f.after + f.vat_amt,
       grand_total_rounded  = f.after + f.vat_amt,
       rounding_adjustment  = 0,
       vat_included         = true
  FROM fin2 f
 WHERE h.running = f.running;

-- 2. ตรวจผล
SELECT COUNT(*) AS updated_rows FROM tbt_sale_invoice_header
 WHERE vat_included = true AND running IN (SELECT running FROM bak_20261128_invoice_money);
SELECT COUNT(*) AS backed_up FROM bak_20261128_invoice_money;
SELECT running, sub_total, vat_amount, grand_total_rounded FROM tbt_sale_invoice_header
 WHERE running IN ('INV261118022', 'INV261112012', 'INV261113002');

COMMIT;
