-- TK202611160005 : กู้แถวพลอยจากใบเบิกที่หายจากใบงาน (บั๊กบันทึกแก้ไขขั้นคัดพลอย/แต่ง ลบแถวพลอยจากใบเบิกทิ้ง)
-- ⚠️ รันหลัง deploy API ของ TK202611160005 แล้วเท่านั้น (ไม่งั้นกดบันทึกแก้ไขอีกครั้งแถวที่กู้จะหายซ้ำ)
-- ขอบเขต: ใบเบิก (type 7) 29 รายการ ของใบงานที่ยังไม่สำเร็จ/ไม่หลอม (status < 100)
--   - ไม่รวม 6910001-2 (แยกตรวจต่างหาก)
--   - ไม่รวมใบที่มีรหัสพลอยเดียวกันอยู่ในใบงานแล้ว (ร้านคีย์กลับเองแล้ว เช่น 6908023-10) กันซ้ำ
-- item_no ใช้รูป G-{planId}-{status}R20261116{seq} — มี R คั่น ไม่ชนเลขที่ระบบออกเอง และใช้ถอยกลับได้
-- ชื่อ/ราคาพลอยคิดแบบเดียวกับ API ตอนเบิก (ReceiptAndIssueStockGemService)

BEGIN;

-- 1) ดูก่อน (ควรได้ 29 แถว)
WITH tx AS (
    SELECT t.id AS tx_id, t.running, t.code, t.qty, t.qty_weight, t.request_date, t.operator_by,
           p.id AS plan_id, p.wo, p.wo_number,
           CASE WHEN EXISTS (SELECT 1 FROM tbm_casting_material m
                             WHERE m.is_active AND (g.shape ILIKE '%' || m.code || '%' OR g.group_name ILIKE '%' || m.code || '%'))
                THEN 50 ELSE 70 END AS target_status,
           g.id AS gem_id,
           g.code || '-' || COALESCE(g.group_name, '') || '-' || COALESCE(g.shape, '') || '-' || COALESCE(g.size, '') || '-' || COALESCE(g.grade, '') AS gem_name,
           CASE WHEN g.unit_code = 'K' THEN g.price ELSE g.price_qty END AS gem_price
    FROM tbt_stock_gem_transection t
    JOIN tbt_production_plan p ON p.wo = t.production_plan_wo AND p.wo_number = t.production_plan_wo_number
    JOIN tbt_stock_gem g ON g.code = t.code
    WHERE t.type = 7
      AND p.status < 100
      AND t.id IN (12941,12965,13121,13185,13192,13275,13369,13417,13418,13419,13420,13421,13423,13426,13427,
                   26908,35267,35268,41585,42858,43676,44095,46674,51740,52113,52686,53295,53589,54684)
)
SELECT tx.wo || '-' || tx.wo_number AS plan, tx.target_status, h.id AS header_id, tx.running, tx.code, tx.qty, tx.qty_weight, tx.gem_price
FROM tx
JOIN tbt_production_plan_status_header h ON h.production_plan_id = tx.plan_id AND h.status = tx.target_status AND h.is_active
WHERE NOT EXISTS (SELECT 1 FROM tbt_production_plan_status_detail_gem d
                  WHERE d.production_plan_id = tx.plan_id AND d.gem_code = tx.code)
ORDER BY plan, tx.tx_id;

-- 1.1) กันซ้ำ: ต้องไม่มีแถวไหน (ผลต้องว่าง) — ถ้ามี = ใบงานมี header สถานะเดียวกันซ้ำ หยุดแล้วแจ้ง dev
SELECT h.production_plan_id, h.status, count(*)
FROM tbt_production_plan_status_header h
JOIN tbt_production_plan p ON p.id = h.production_plan_id
JOIN tbt_stock_gem_transection t ON t.production_plan_wo = p.wo AND t.production_plan_wo_number = p.wo_number
WHERE h.is_active AND h.status IN (50, 70)
  AND t.id IN (12941,12965,13121,13185,13192,13275,13369,13417,13418,13419,13420,13421,13423,13426,13427,
               26908,35267,35268,41585,42858,43676,44095,46674,51740,52113,52686,53295,53589,54684)
GROUP BY h.production_plan_id, h.status, t.id
HAVING count(*) > 1;

-- หมายเหตุ: ราคาพลอย (gem_price) ใช้ราคาปัจจุบันในคลัง ไม่ใช่ราคา ณ วันเบิก (ประวัติเบิกไม่เก็บราคาไว้)

-- 2) ใส่กลับ
WITH tx AS (
    SELECT t.id AS tx_id, t.running, t.code, t.qty, t.qty_weight, t.request_date, t.operator_by,
           p.id AS plan_id,
           CASE WHEN EXISTS (SELECT 1 FROM tbm_casting_material m
                             WHERE m.is_active AND (g.shape ILIKE '%' || m.code || '%' OR g.group_name ILIKE '%' || m.code || '%'))
                THEN 50 ELSE 70 END AS target_status,
           g.id AS gem_id,
           g.code || '-' || COALESCE(g.group_name, '') || '-' || COALESCE(g.shape, '') || '-' || COALESCE(g.size, '') || '-' || COALESCE(g.grade, '') AS gem_name,
           CASE WHEN g.unit_code = 'K' THEN g.price ELSE g.price_qty END AS gem_price
    FROM tbt_stock_gem_transection t
    JOIN tbt_production_plan p ON p.wo = t.production_plan_wo AND p.wo_number = t.production_plan_wo_number
    JOIN tbt_stock_gem g ON g.code = t.code
    WHERE t.type = 7
      AND p.status < 100
      AND t.id IN (12941,12965,13121,13185,13192,13275,13369,13417,13418,13419,13420,13421,13423,13426,13427,
                   26908,35267,35268,41585,42858,43676,44095,46674,51740,52113,52686,53295,53589,54684)
), src AS (
    SELECT tx.*, h.id AS header_id,
           ROW_NUMBER() OVER (PARTITION BY tx.plan_id, tx.target_status ORDER BY tx.tx_id) AS seq
    FROM tx
    JOIN tbt_production_plan_status_header h ON h.production_plan_id = tx.plan_id AND h.status = tx.target_status AND h.is_active
    WHERE NOT EXISTS (SELECT 1 FROM tbt_production_plan_status_detail_gem d
                      WHERE d.production_plan_id = tx.plan_id AND d.gem_code = tx.code)
)
INSERT INTO tbt_production_plan_status_detail_gem
    (production_plan_id, item_no, is_active, header_id, request_date,
     gem_id, gem_code, gem_qty, gem_weight, gem_name, gem_price,
     outbound_running, outbound_name, outbound_date)
SELECT plan_id,
       'G-' || plan_id || '-' || target_status || 'R20261116' || LPAD(seq::text, 4, '0'),
       TRUE, header_id, request_date,
       gem_id, code, qty, qty_weight, gem_name, gem_price,
       running, operator_by, request_date
FROM src
ON CONFLICT (production_plan_id, item_no) DO NOTHING;
-- ควรขึ้น INSERT 0 29

-- 3) ตรวจผล แล้วค่อย COMMIT (ผิด → ROLLBACK)
SELECT production_plan_id, header_id, item_no, gem_code, gem_qty, gem_weight, outbound_running
FROM tbt_production_plan_status_detail_gem
WHERE item_no LIKE 'G-%R20261116%'
ORDER BY production_plan_id, item_no;

-- COMMIT;
