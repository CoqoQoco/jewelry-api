-- TK202611190003 rollback : คืน price_weight เป็น NULL เฉพาะแถวที่ backfill เติมไว้ (และยังเป็นค่าเดิมที่เติม)
BEGIN;

UPDATE tbt_stock_piece_material pm
SET price_weight = NULL
FROM tbt_stock_piece_material_price_weight_backfill_20261119 b
WHERE b.id = pm.id AND pm.price_weight = b.price_weight;

DROP TABLE IF EXISTS tbt_stock_piece_material_price_weight_backfill_20261119;

COMMIT;
