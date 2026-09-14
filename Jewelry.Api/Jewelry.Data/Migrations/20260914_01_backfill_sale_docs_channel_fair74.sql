-- =============================================
-- Migration: Backfill sale_channel_code = FAIR74 for Bangkok Gems & Jewelry Fair #74
-- Date: 2026-09-14
-- Description: แก้ sale_channel_code ของใบสั่งขาย/ใบแจ้งหนี้ที่เกิดขึ้นช่วงงานแสดงสินค้า
--              Bangkok Gems & Jewelry Fair #74 (8-12 ก.ย. 2569) ให้เป็น FAIR74 ทั้งหมด —
--              วันที่ 8-11 ก.ย. ยังไม่ deploy feature (sale_channel_code เป็น NULL),
--              วันที่ 12 ก.ย. ติด SHOP ผิดจาก bug ค่า default ของ dropdown
--              กฎธุรกิจ: ใบสั่งขายกับใบแจ้งหนี้ของมันต้อง sale channel เดียวกันเสมอ
--              จึงหาชุดใบสั่งขายเป้าหมายก่อน แล้วให้ใบแจ้งหนี้ตามใบสั่งขายของตัวเอง
-- Run order: รันหลัง 20260910_03_create_tbm_sale_channel.sql ถึง
--            20260910_05_alter_sale_docs_add_channel_due_date.sql
--            (tbm_sale_channel ต้องมีแถว FAIR74 อยู่แล้ว)
-- Re-run safety: Idempotent — UPDATE เช็ค IS DISTINCT FROM 'FAIR74' ก่อนแก้เสมอ
--                รันซ้ำแล้วไม่มีแถวให้แก้เพิ่ม ไม่กระทบข้อมูล
-- =============================================

BEGIN;

-- =============================================
-- 1. ชุดใบสั่งขายเป้าหมาย = SO ที่เปิดช่วงงาน UNION SO ที่มีใบแจ้งหนี้ออกช่วงงาน
-- =============================================
CREATE TEMP TABLE tmp_fair74_so_target (
    so_number CHARACTER VARYING PRIMARY KEY
) ON COMMIT DROP;

INSERT INTO tmp_fair74_so_target (so_number)
SELECT so.so_number
FROM tbt_sale_order so
WHERE so.create_date >= '2026-09-07T17:00:00Z'
  AND so.create_date < '2026-09-12T17:00:00Z'
UNION
SELECT h.so_running
FROM tbt_sale_invoice_header h
WHERE h.so_running IS NOT NULL
  AND h.create_date >= '2026-09-07T17:00:00Z'
  AND h.create_date < '2026-09-12T17:00:00Z';

-- =============================================
-- 2. ตรวจสอบก่อนแก้ (Before) — นับจำนวนแยกตาม channel เดิม
-- =============================================
-- ใบสั่งขายเป้าหมาย
SELECT coalesce(so.sale_channel_code, '(null)') AS sale_channel_code, count(*) AS cnt
FROM tbt_sale_order so
WHERE so.so_number IN (SELECT so_number FROM tmp_fair74_so_target)
GROUP BY 1
ORDER BY 1;

-- ใบแจ้งหนี้เป้าหมาย (so_running อยู่ในชุดเป้าหมาย หรือออกช่วงงานแสดงสินค้าเอง)
SELECT coalesce(h.sale_channel_code, '(null)') AS sale_channel_code, count(*) AS cnt
FROM tbt_sale_invoice_header h
WHERE h.so_running IN (SELECT so_number FROM tmp_fair74_so_target)
   OR (h.create_date >= '2026-09-07T17:00:00Z' AND h.create_date < '2026-09-12T17:00:00Z')
GROUP BY 1
ORDER BY 1;

-- =============================================
-- 3. แก้ sale_channel_code ของใบสั่งขาย
-- =============================================
UPDATE tbt_sale_order
SET sale_channel_code = 'FAIR74'
WHERE so_number IN (SELECT so_number FROM tmp_fair74_so_target)
  AND sale_channel_code IS DISTINCT FROM 'FAIR74';

-- =============================================
-- 4. แก้ sale_channel_code ของใบแจ้งหนี้ (รวมใบที่ถูกลบ is_delete = true ด้วย
--    เพราะกฎธุรกิจต้องให้ channel ตรงกับ SO เสมอไม่ว่าใบจะถูกลบไปแล้วหรือไม่)
--    ห้ามแตะ update_by / update_date — สำหรับใบที่ถูกลบ ฟิลด์นี้บันทึกว่าใครลบใบไปแล้ว
--    เมื่อไหร่ การเขียนทับจะทำลายประวัติการลบทิ้ง จึงแก้เฉพาะ sale_channel_code เท่านั้น
-- =============================================
UPDATE tbt_sale_invoice_header
SET sale_channel_code = 'FAIR74'
WHERE (
        so_running IN (SELECT so_number FROM tmp_fair74_so_target)
        OR (create_date >= '2026-09-07T17:00:00Z' AND create_date < '2026-09-12T17:00:00Z')
      )
  AND sale_channel_code IS DISTINCT FROM 'FAIR74';

-- =============================================
-- 5. ตรวจสอบหลังแก้ (After) — ค่าที่คาดหวัง ณ วันที่เช็ค 2026-09-14
-- =============================================
-- ใบสั่งขายเป้าหมาย — คาดหวัง 1 แถว: FAIR74, cnt = 49
SELECT coalesce(so.sale_channel_code, '(null)') AS sale_channel_code, count(*) AS cnt
FROM tbt_sale_order so
WHERE so.so_number IN (SELECT so_number FROM tmp_fair74_so_target)
GROUP BY 1
ORDER BY 1;

-- ใบแจ้งหนี้เป้าหมาย — คาดหวัง channel = FAIR74 ทั้งหมด รวม 60 แถว
-- แยก is_delete: is_delete=false (ใบยัง active) = 36 แถว, is_delete=true (ใบถูกลบ) = 24 แถว
SELECT coalesce(h.sale_channel_code, '(null)') AS sale_channel_code,
       coalesce(h.is_delete, false) AS is_delete,
       count(*) AS cnt
FROM tbt_sale_invoice_header h
WHERE h.so_running IN (SELECT so_number FROM tmp_fair74_so_target)
   OR (h.create_date >= '2026-09-07T17:00:00Z' AND h.create_date < '2026-09-12T17:00:00Z')
GROUP BY 1, 2
ORDER BY 1, 2;

-- =============================================
-- 6. ตรวจสอบทั้งตาราง — ใบแจ้งหนี้ต้อง channel ตรงกับใบสั่งขายของตัวเองเสมอ
--    คาดหวัง 0 แถว
-- =============================================
SELECT h.running, h.so_running, h.sale_channel_code AS invoice_channel, so.sale_channel_code AS so_channel
FROM tbt_sale_invoice_header h
JOIN tbt_sale_order so ON so.so_number = h.so_running
WHERE h.sale_channel_code IS DISTINCT FROM so.sale_channel_code;

COMMIT;
