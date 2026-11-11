-- =============================================
-- Rollback: Fix legacy silver material rows [TK202611110001]
-- Date: 2026-11-11
-- Description: คืนค่าแถวจาก bak_20261111_silver_material ตาม id
-- วิธีรัน: DBeaver ปิด auto-commit -> รันตั้งแต่ BEGIN ถึง SELECT ตรวจผล -> ตรวจแล้วรัน COMMIT; (หรือ ROLLBACK;) ด้วยมือ
-- =============================================

BEGIN;

UPDATE tbt_stock_piece_material m
SET type = b.type,
    type_code = b.type_code,
    type_barcode = b.type_barcode,
    qty = b.qty,
    update_date = b.update_date,
    update_by = b.update_by
FROM bak_20261111_silver_material b
WHERE m.id = b.id;

SELECT count(*) AS restored
FROM tbt_stock_piece_material m
JOIN bak_20261111_silver_material b ON b.id = m.id
WHERE m.type IS NOT DISTINCT FROM b.type AND m.type_code IS NOT DISTINCT FROM b.type_code;

-- COMMIT;
-- ROLLBACK;

-- เมื่อมั่นใจแล้วค่อยลบ backup เอง: DROP TABLE bak_20261111_silver_material;
