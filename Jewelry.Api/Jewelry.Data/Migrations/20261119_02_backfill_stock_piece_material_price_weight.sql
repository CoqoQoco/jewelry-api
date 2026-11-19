-- TK202611190003 : backfill tbt_stock_piece_material.price_weight จาก tbt_stock_product_receipt_plan.json_draft
-- ต้องรัน 20261119_01_add_price_weight_to_stock_piece_material.sql ก่อน
--
-- วิธีใช้:
--   1) รันส่วน DRY-RUN (SELECT) ด้านล่างก่อน ดูจำนวนและตัวอย่างแถวที่จะถูกเติม
--   2) ถ้าพอใจ รันส่วน BACKUP + UPDATE (อยู่ใน BEGIN/COMMIT)
--
-- กติกา: เฉพาะ Gold, isReceipt = true, qtyWeightPrice > 0, แถวที่ price_weight IS NULL
--   ข้ามกรณีกำกวม: (stock_number, product_code, type_code) ที่ได้ qtyWeightPrice มากกว่า 1 ค่า
--   หรือชิ้นนั้นมีแถว Gold type_code เดียวกันมากกว่า 1 แถว
-- ข้ามแถว top-up (topUpStockNumber ไม่ว่าง) เพราะไม่ได้เขียนวัสดุของล็อตนั้น
-- รองรับ json ทั้ง camelCase และ PascalCase

-- =========================== DRY-RUN ===========================
/*
WITH src AS (
    SELECT
        COALESCE(s ->> 'stockNumber', s ->> 'StockNumber')                 AS stock_number,
        upper(COALESCE(s ->> 'productNumber', s ->> 'ProductNumber'))      AS product_code,
        COALESCE(m ->> 'typeCode', m ->> 'TypeCode')                       AS type_code,
        CASE WHEN COALESCE(m ->> 'qtyWeightPrice', m ->> 'QtyWeightPrice') ~ '^[0-9]+(\.[0-9]+)?$'
             THEN COALESCE(m ->> 'qtyWeightPrice', m ->> 'QtyWeightPrice')::numeric END AS qty_weight_price
    FROM tbt_stock_product_receipt_plan p
    CROSS JOIN LATERAL jsonb_array_elements(
        CASE WHEN p.json_draft IS NOT NULL AND jsonb_typeof(p.json_draft::jsonb) = 'array'
             THEN p.json_draft::jsonb ELSE '[]'::jsonb END) s
    CROSS JOIN LATERAL jsonb_array_elements(
        CASE WHEN jsonb_typeof(COALESCE(s -> 'materials', s -> 'Materials')) = 'array'
             THEN COALESCE(s -> 'materials', s -> 'Materials') ELSE '[]'::jsonb END) m
    WHERE lower(COALESCE(m ->> 'type', m ->> 'Type')) = 'gold'
      AND lower(COALESCE(s ->> 'isReceipt', s ->> 'IsReceipt', 'false')) = 'true'
      AND NULLIF(COALESCE(s ->> 'topUpStockNumber', s ->> 'TopUpStockNumber'), '') IS NULL
),
pos AS (
    SELECT stock_number, product_code, type_code, MIN(qty_weight_price) AS price_weight
    FROM src
    WHERE qty_weight_price > 0
    GROUP BY stock_number, product_code, type_code
    HAVING COUNT(DISTINCT qty_weight_price) = 1
),
dup AS (
    SELECT stock_number, upper(product_code) AS product_code, type_code
    FROM tbt_stock_piece_material
    WHERE lower(type) = 'gold'
    GROUP BY stock_number, upper(product_code), type_code
    HAVING COUNT(*) > 1
)
SELECT COUNT(*) AS will_update FROM tbt_stock_piece_material pm
JOIN pos ON pos.stock_number = pm.stock_number AND pos.product_code = upper(pm.product_code) AND pos.type_code = pm.type_code
WHERE lower(pm.type) = 'gold' AND pm.price_weight IS NULL
  AND NOT EXISTS (SELECT 1 FROM dup d WHERE d.stock_number = pm.stock_number AND d.product_code = upper(pm.product_code) AND d.type_code = pm.type_code);
-- ตัวอย่างแถว: เปลี่ยน SELECT COUNT(*) AS will_update เป็น SELECT pm.id, pm.stock_number, pm.product_code, pm.type_code, pm.weight, pm.price, pos.price_weight ... LIMIT 50
*/

-- ====================== BACKUP + UPDATE =======================
BEGIN;

CREATE TABLE IF NOT EXISTS tbt_stock_piece_material_price_weight_backfill_20261119 (
    id bigint PRIMARY KEY,
    price_weight numeric(18,2) NOT NULL
);

WITH src AS (
    SELECT
        COALESCE(s ->> 'stockNumber', s ->> 'StockNumber')                 AS stock_number,
        upper(COALESCE(s ->> 'productNumber', s ->> 'ProductNumber'))      AS product_code,
        COALESCE(m ->> 'typeCode', m ->> 'TypeCode')                       AS type_code,
        CASE WHEN COALESCE(m ->> 'qtyWeightPrice', m ->> 'QtyWeightPrice') ~ '^[0-9]+(\.[0-9]+)?$'
             THEN COALESCE(m ->> 'qtyWeightPrice', m ->> 'QtyWeightPrice')::numeric END AS qty_weight_price
    FROM tbt_stock_product_receipt_plan p
    CROSS JOIN LATERAL jsonb_array_elements(
        CASE WHEN p.json_draft IS NOT NULL AND jsonb_typeof(p.json_draft::jsonb) = 'array'
             THEN p.json_draft::jsonb ELSE '[]'::jsonb END) s
    CROSS JOIN LATERAL jsonb_array_elements(
        CASE WHEN jsonb_typeof(COALESCE(s -> 'materials', s -> 'Materials')) = 'array'
             THEN COALESCE(s -> 'materials', s -> 'Materials') ELSE '[]'::jsonb END) m
    WHERE lower(COALESCE(m ->> 'type', m ->> 'Type')) = 'gold'
      AND lower(COALESCE(s ->> 'isReceipt', s ->> 'IsReceipt', 'false')) = 'true'
      AND NULLIF(COALESCE(s ->> 'topUpStockNumber', s ->> 'TopUpStockNumber'), '') IS NULL
),
pos AS (
    SELECT stock_number, product_code, type_code, MIN(qty_weight_price) AS price_weight
    FROM src
    WHERE qty_weight_price > 0
    GROUP BY stock_number, product_code, type_code
    HAVING COUNT(DISTINCT qty_weight_price) = 1
),
dup AS (
    SELECT stock_number, upper(product_code) AS product_code, type_code
    FROM tbt_stock_piece_material
    WHERE lower(type) = 'gold'
    GROUP BY stock_number, upper(product_code), type_code
    HAVING COUNT(*) > 1
)
INSERT INTO tbt_stock_piece_material_price_weight_backfill_20261119 (id, price_weight)
SELECT pm.id, pos.price_weight
FROM tbt_stock_piece_material pm
JOIN pos ON pos.stock_number = pm.stock_number AND pos.product_code = upper(pm.product_code) AND pos.type_code = pm.type_code
WHERE lower(pm.type) = 'gold' AND pm.price_weight IS NULL
  AND NOT EXISTS (SELECT 1 FROM dup d WHERE d.stock_number = pm.stock_number AND d.product_code = upper(pm.product_code) AND d.type_code = pm.type_code)
ON CONFLICT (id) DO NOTHING;

UPDATE tbt_stock_piece_material pm
SET price_weight = b.price_weight
FROM tbt_stock_piece_material_price_weight_backfill_20261119 b
WHERE b.id = pm.id AND pm.price_weight IS NULL;

-- ตรวจจำนวนก่อน COMMIT
SELECT COUNT(*) AS filled FROM tbt_stock_piece_material_price_weight_backfill_20261119;

COMMIT;
