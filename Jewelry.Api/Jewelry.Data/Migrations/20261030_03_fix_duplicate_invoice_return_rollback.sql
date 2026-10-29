-- Rollback: 20261030_03_fix_duplicate_invoice_return.sql
-- นำแถว RETURN / INVOICE_DELETE ที่ถูกลบกลับจากตารางสำรอง (คง id เดิม)
BEGIN;

INSERT INTO tbt_stock_movement
SELECT b.*
FROM tbt_stock_movement_dup_return_bak b
WHERE NOT EXISTS (SELECT 1 FROM tbt_stock_movement m WHERE m.id = b.id);

INSERT INTO tbt_stock_movement_unit
SELECT b.*
FROM tbt_stock_movement_unit_dup_return_bak b
WHERE NOT EXISTS (SELECT 1 FROM tbt_stock_movement_unit mu WHERE mu.id = b.id);

-- ตรวจ: ต้องไม่มีแถวสำรองที่ยังไม่กลับเข้าตารางหลัก — expected 0
SELECT COUNT(*) AS not_restored
FROM tbt_stock_movement_dup_return_bak b
WHERE NOT EXISTS (SELECT 1 FROM tbt_stock_movement m WHERE m.id = b.id);

COMMIT;

-- เมื่อมั่นใจแล้วค่อยลบตารางสำรองเอง:
-- DROP TABLE IF EXISTS tbt_stock_movement_unit_dup_return_bak;
-- DROP TABLE IF EXISTS tbt_stock_movement_dup_return_bak;
