-- =============================================
-- Migration: Fix duplicate POS auto-payment rows (commit b844097 bug)
-- Date: 2026-09-12
-- Description: commit b844097 เพิ่มการบันทึกรับเงินอัตโนมัติใน InvoiceService.Create
--              เมื่อ request.Payment เป็น 1/2/4 แต่ PosCheckoutService.Checkout
--              เรียก _invoiceService.Create(...) แล้ววนเรียก _invoiceService.CreatePayment(...)
--              ต่ออีกรอบ ทำให้บิล POS วันที่ 2026-09-12 จำนวน 8 ใบ มียอดรับเงินซ้ำสองเท่า
--              โค้ด auto-payment ถูกลบออกจาก InvoiceService.Create แล้ว
--              (ดู Jewelry.Service/Sale/Invoice/InvoiceService.cs)
--              สคริปต์นี้ soft-delete เฉพาะแถว auto-payment ที่ซ้ำ 8 แถว
--              ไม่แตะแถว payment ที่ถูกต้องซึ่งเกิดจาก PosCheckoutService
-- Excluded (ห้ามแตะ):
--   PAY-INV260912012260912001 — มีคนลบแถวซ้ำไปแล้ว เหลือแถวเดียว ลบซ้ำบิลจะกลายเป็นยังไม่จ่าย
--   PAY-INV260912003260912001 — ไม่ใช่ POS ไม่ซ้ำ แต่ยอดขาด 0.60 จาก migration เลิกปัดเศษ
--                                (20260912_01_backfill_sale_docs_money_no_rounding.sql) ที่รันทีหลัง
--                                บิลนี้ออก — มีคำสั่งสำรอง (comment out) อยู่ท้ายไฟล์ ต้องยืนยันก่อนรัน
-- Re-run safe: ทุก UPDATE guard ด้วย is_delete = false — รันซ้ำแล้ว no-op
-- =============================================

-- =============================================
-- BEFORE — ตรวจยอดรับเงินรวมเทียบยอดบิลของใบที่เกี่ยวข้องทั้งหมด (10 ใบ: 8 ใบที่แก้ + 2 ใบที่ห้ามแตะ)
-- =============================================
WITH context_payments AS (
    SELECT running, invoice_running
    FROM tbt_sale_invoice_payment_item
    WHERE running IN (
        'PAY-INV260912004260912001',
        'PAY-INV260912005260912001',
        'PAY-INV260912006260912001',
        'PAY-INV260912007260912001',
        'PAY-INV260912008260912001',
        'PAY-INV260912009260912001',
        'PAY-INV260912010260912001',
        'PAY-INV260912011260912001',
        'PAY-INV260912012260912001', -- ห้ามลบ
        'PAY-INV260912003260912001'  -- ห้ามลบ
    )
)
SELECT
    h.running AS invoice_running,
    h.deposit,
    h.grand_total_rounded,
    (h.grand_total_rounded - COALESCE(h.deposit, 0)) AS amount_due,
    COALESCE(SUM(pi.amount) FILTER (WHERE pi.is_delete = false), 0) AS total_received_active,
    COUNT(*) FILTER (WHERE pi.is_delete = false) AS active_payment_rows
FROM tbt_sale_invoice_header h
JOIN tbt_sale_invoice_payment_item pi ON pi.invoice_running = h.running
WHERE h.running IN (SELECT DISTINCT invoice_running FROM context_payments)
GROUP BY h.running, h.deposit, h.grand_total_rounded
ORDER BY h.running;

-- =============================================
-- FIX — soft delete แถว auto-payment ที่ซ้ำ ทีละ running (8 แถวเท่านั้น)
-- =============================================
UPDATE tbt_sale_invoice_payment_item
SET is_delete = true, update_by = 'system-fix', update_date = now()
WHERE running = 'PAY-INV260912004260912001'
  AND is_delete = false;

UPDATE tbt_sale_invoice_payment_item
SET is_delete = true, update_by = 'system-fix', update_date = now()
WHERE running = 'PAY-INV260912005260912001'
  AND is_delete = false;

UPDATE tbt_sale_invoice_payment_item
SET is_delete = true, update_by = 'system-fix', update_date = now()
WHERE running = 'PAY-INV260912006260912001'
  AND is_delete = false;

UPDATE tbt_sale_invoice_payment_item
SET is_delete = true, update_by = 'system-fix', update_date = now()
WHERE running = 'PAY-INV260912007260912001'
  AND is_delete = false;

UPDATE tbt_sale_invoice_payment_item
SET is_delete = true, update_by = 'system-fix', update_date = now()
WHERE running = 'PAY-INV260912008260912001'
  AND is_delete = false;

UPDATE tbt_sale_invoice_payment_item
SET is_delete = true, update_by = 'system-fix', update_date = now()
WHERE running = 'PAY-INV260912009260912001'
  AND is_delete = false;

UPDATE tbt_sale_invoice_payment_item
SET is_delete = true, update_by = 'system-fix', update_date = now()
WHERE running = 'PAY-INV260912010260912001'
  AND is_delete = false;

UPDATE tbt_sale_invoice_payment_item
SET is_delete = true, update_by = 'system-fix', update_date = now()
WHERE running = 'PAY-INV260912011260912001'
  AND is_delete = false;

-- =============================================
-- AFTER — รันซ้ำ query เดิมเพื่อเทียบผลก่อน/หลัง
-- =============================================
WITH context_payments AS (
    SELECT running, invoice_running
    FROM tbt_sale_invoice_payment_item
    WHERE running IN (
        'PAY-INV260912004260912001',
        'PAY-INV260912005260912001',
        'PAY-INV260912006260912001',
        'PAY-INV260912007260912001',
        'PAY-INV260912008260912001',
        'PAY-INV260912009260912001',
        'PAY-INV260912010260912001',
        'PAY-INV260912011260912001',
        'PAY-INV260912012260912001', -- ห้ามลบ
        'PAY-INV260912003260912001'  -- ห้ามลบ
    )
)
SELECT
    h.running AS invoice_running,
    h.deposit,
    h.grand_total_rounded,
    (h.grand_total_rounded - COALESCE(h.deposit, 0)) AS amount_due,
    COALESCE(SUM(pi.amount) FILTER (WHERE pi.is_delete = false), 0) AS total_received_active,
    COUNT(*) FILTER (WHERE pi.is_delete = false) AS active_payment_rows
FROM tbt_sale_invoice_header h
JOIN tbt_sale_invoice_payment_item pi ON pi.invoice_running = h.running
WHERE h.running IN (SELECT DISTINCT invoice_running FROM context_payments)
GROUP BY h.running, h.deposit, h.grand_total_rounded
ORDER BY h.running;

-- =============================================
-- สำรอง (ห้ามรันจนกว่าจะยืนยันกับลูกค้า/พนักงานว่ารับเงินครบ 9,689.60 จริงหรือไม่)
-- PAY-INV260912003260912001 บันทึกไว้ 9,689 แต่ยอดบิลคือ 9,689.60 (ขาด 0.60)
-- เกิดจาก migration เลิกปัดเศษ (20260912_01_backfill_sale_docs_money_no_rounding.sql)
-- รันทีหลังบิลนี้ออก ทำให้ยอดบิลขยับจากที่เคยปัดเป็น 9,689 ไปเป็น 9,689.60
-- ต้องถามยืนยันก่อนว่าลูกค้าจ่ายจริง 9,689.60 หรือ 9,689 — ถ้าใช่ 9,689.60 ค่อยแก้ไข update_by
-- =============================================
-- UPDATE tbt_sale_invoice_payment_item
-- SET amount = 9689.60, update_by = 'system-fix', update_date = now()
-- WHERE running = 'PAY-INV260912003260912001'
--   AND is_delete = false;
