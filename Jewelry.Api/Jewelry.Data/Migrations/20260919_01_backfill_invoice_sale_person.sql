-- Migration: Backfill invoice SALE/SUPPORT from sale order
-- Date: 2026-09-19
-- Description: ใบแจ้งหนี้เก็บ sale_person / sale_support ของตัวเอง (คัดลอกตอนสร้าง)
--              แต่ที่ร้านกรอกชื่อที่ sale order ทีหลังออกบิล ทำให้ใบเก่าว่าง
--              migration นี้ให้ SO เป็นต้นทาง: SO มีค่า -> เขียนทับ, SO ว่าง -> คงของเดิมบนใบ
--              คาดหวัง UPDATE 12 แถว (10 ใบเติมช่องว่าง + 2 ใบสลับ SALE<->SUPPORT)

BEGIN;

-- 1) snapshot ก่อนแก้ (ย้อนกลับได้)
CREATE TABLE IF NOT EXISTS bak_20260919_invoice_sale_person AS
SELECT running, sale_person, sale_support, sale_person_username
FROM   tbt_sale_invoice_header;

-- 2) SO ชนะทุกช่องที่ SO มีค่า
UPDATE tbt_sale_invoice_header i
SET    sale_person          = COALESCE(NULLIF(so.sale_person,''),          i.sale_person),
       sale_support         = COALESCE(NULLIF(so.sale_support,''),         i.sale_support),
       sale_person_username = COALESCE(NULLIF(so.sale_person_username,''), i.sale_person_username)
FROM   tbt_sale_order so
WHERE  so.so_number = i.so_running
  AND  i.is_delete = false
  AND  ( (COALESCE(so.sale_person,'') <> '' AND COALESCE(i.sale_person,'') <> COALESCE(so.sale_person,''))
      OR (COALESCE(so.sale_support,'') <> '' AND COALESCE(i.sale_support,'') <> COALESCE(so.sale_support,''))
      OR (COALESCE(so.sale_person_username,'') <> '' AND COALESCE(i.sale_person_username,'') <> COALESCE(so.sale_person_username,'')) );

-- 3) verify: ต้องได้ 21 / 20 / (จำนวนใบทั้งหมด ณ ตอนรัน)
SELECT count(*) FILTER (WHERE COALESCE(sale_person,'') <> '')  AS person_filled,
       count(*) FILTER (WHERE COALESCE(sale_support,'') <> '') AS support_filled,
       count(*)                                                AS total
FROM   tbt_sale_invoice_header
WHERE  is_delete = false;

COMMIT;
