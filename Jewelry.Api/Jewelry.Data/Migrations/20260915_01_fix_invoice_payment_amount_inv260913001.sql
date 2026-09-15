-- =============================================
-- Migration: Fix underpaid payment row on invoice INV260913001
-- Date: 2026-09-15
-- Description: หน้าโมดัลรับเงิน (payment modal) บนเว็บอ่านฟิลด์ grandTotal ที่ไม่มีจริง
--              ทำให้ยอดใบเสร็จ/ยอดคงเหลือ (remaining) โชว์ 0.00 พนักงานจึงต้องพิมพ์ยอดเอง
--              เมื่อคีย์บิล INV260913001 (SO260910014, ลูกค้า HARISH, สกุล US$)
--              พนักงาน (chomsang) คีย์ยอดรับเงิน 9,689 แต่ยอดบิลจริง (grand_total_rounded)
--              คือ 9,689.60 ทำให้แถว payment PAY-INV260913001260915001 ขาดไป 0.60
--              และบิลนี้ค้างสถานะจ่ายไม่ครบ (partially paid) ทั้งที่ควรจ่ายครบแล้ว
--              บั๊กหน้าเว็บกำลังแก้แยกอยู่ที่ jeweley-ui (ไม่เกี่ยวกับสคริปต์นี้)
-- History: SO260910014 ออกบิลมาแล้ว 4 ครั้ง บิลก่อนหน้า INV260912003 (ถูกลบไปแล้ว)
--          ก็เคยเจอปัญหาเดียวกัน (รับเงิน 9,689 vs ยอดบิล 9,689.60) ซึ่งเกิดจาก
--          migration 20260912_01_backfill_sale_docs_money_no_rounding.sql ที่รันขยับ
--          ยอดบิลจากที่เคยปัดเศษเป็น 9,689 ไปเป็น 9,689.60 ทีหลังบิลนั้นออก
--          ไฟล์ 20260912_03_fix_duplicate_pos_auto_payment.sql เคยเตรียมคำสั่งแก้ไว้
--          แบบ comment out รอยืนยันก่อน — บิล INV260912003 ถูกลบไปแล้ว ห้ามไปแตะอีก
-- Scope: ตรวจสอบข้ามทุกใบเสร็จที่ active บน prod แล้ว มีเฉพาะ INV260913001 ใบเดียว
--        ที่ยอดรับเงิน active รวมขาดจาก grand_total_rounded มากกว่า 0.005
-- ⚠️ คำเตือนสำคัญ: ต้องให้ร้านยืนยันกับลูกค้าก่อนว่าลูกค้าจ่ายจริง 9,689.60 US$
--    (ไม่ใช่ 9,689) — เพราะบิลก่อนหน้า INV260912003 เคยโชว์ยอด 9,689 ก่อนที่
--    migration เลิกปัดเศษจะรัน จึงมีความเป็นไปได้ที่ลูกค้าจะจ่ายมาแค่ 9,689 จริงๆ
--    ห้ามรันสคริปต์นี้จนกว่าจะยืนยันกับร้าน/พนักงานที่รับเงินแล้วเท่านั้น
-- Not touched: tbt_sale_invoice_header.payment / paymant_name เป็น snapshot
--              ตอนออกบิล (billing time) ห้าม sync ตามยอด payment_item ทีหลังเด็ดขาด
--              สคริปต์นี้แก้เฉพาะ tbt_sale_invoice_payment_item เท่านั้น
-- Re-run safe: UPDATE guard ด้วย amount = 9689 AND is_delete = false —
--              รันซ้ำหลัง fix ไปแล้ว amount จะเป็น 9689.60 ไม่ตรง WHERE จึง no-op
-- =============================================

-- =============================================
-- BEFORE — ยอดบิล ยอดรับเงิน active และยอดคงเหลือของ INV260913001
-- =============================================
SELECT
    h.running AS invoice_running,
    h.deposit,
    h.grand_total_rounded,
    (h.grand_total_rounded - COALESCE(h.deposit, 0)) AS amount_due,
    COALESCE(SUM(pi.amount) FILTER (WHERE pi.is_delete = false), 0) AS total_received_active,
    COUNT(*) FILTER (WHERE pi.is_delete = false) AS active_payment_rows,
    (h.grand_total_rounded - COALESCE(h.deposit, 0))
        - COALESCE(SUM(pi.amount) FILTER (WHERE pi.is_delete = false), 0) AS outstanding
FROM tbt_sale_invoice_header h
JOIN tbt_sale_invoice_payment_item pi ON pi.invoice_running = h.running
WHERE h.running = 'INV260913001'
GROUP BY h.running, h.deposit, h.grand_total_rounded;

SELECT running, invoice_running, amount, is_delete, remark, update_by, update_date
FROM tbt_sale_invoice_payment_item
WHERE running = 'PAY-INV260913001260915001';

-- =============================================
-- FIX — แก้ยอดแถว payment ที่ขาดจาก 9,689 เป็น 9,689.60 พร้อมบันทึก audit note ใน remark
-- =============================================
UPDATE tbt_sale_invoice_payment_item
SET amount = 9689.60,
    remark = CASE
        WHEN remark IS NULL THEN 'แก้ยอดจาก 9,689 เป็น 9,689.60 (system-fix 2026-09-15)'
        ELSE remark || ' | ' || 'แก้ยอดจาก 9,689 เป็น 9,689.60 (system-fix 2026-09-15)'
    END,
    update_by = 'system-fix',
    update_date = now()
WHERE running = 'PAY-INV260913001260915001'
  AND invoice_running = 'INV260913001'
  AND amount = 9689
  AND is_delete = false;

-- =============================================
-- AFTER — รันซ้ำ query เดิมเพื่อเทียบผลก่อน/หลัง (outstanding ต้องเป็น 0.00)
-- =============================================
SELECT
    h.running AS invoice_running,
    h.deposit,
    h.grand_total_rounded,
    (h.grand_total_rounded - COALESCE(h.deposit, 0)) AS amount_due,
    COALESCE(SUM(pi.amount) FILTER (WHERE pi.is_delete = false), 0) AS total_received_active,
    COUNT(*) FILTER (WHERE pi.is_delete = false) AS active_payment_rows,
    (h.grand_total_rounded - COALESCE(h.deposit, 0))
        - COALESCE(SUM(pi.amount) FILTER (WHERE pi.is_delete = false), 0) AS outstanding
FROM tbt_sale_invoice_header h
JOIN tbt_sale_invoice_payment_item pi ON pi.invoice_running = h.running
WHERE h.running = 'INV260913001'
GROUP BY h.running, h.deposit, h.grand_total_rounded;

SELECT running, invoice_running, amount, is_delete, remark, update_by, update_date
FROM tbt_sale_invoice_payment_item
WHERE running = 'PAY-INV260913001260915001';
