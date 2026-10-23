-- =============================================
-- Rollback: 20261023_03_backfill_outsource_receipt_names_from_codes.sql (TK202610230007)
-- Restore จากตาราง bak_tk202610230007_* ที่สร้างตอนรัน migration
-- =============================================
BEGIN;

UPDATE tbt_sku s
SET production_type      = b.production_type,
    production_type_size = b.production_type_size,
    mold                 = b.mold,
    update_date          = b.update_date,
    update_by            = b.update_by
FROM bak_tk202610230007_sku b
WHERE s.sku_code = b.sku_code;

UPDATE tbt_stock_product_receipt_item i
SET production_type      = b.production_type,
    production_type_size = b.production_type_size,
    update_date          = b.update_date,
    update_by            = b.update_by
FROM bak_tk202610230007_receipt_item b
WHERE i.stock_receipt_number = b.stock_receipt_number;

UPDATE tbt_stock_piece_material m
SET weight_unit = b.weight_unit,
    update_date = b.update_date,
    update_by   = b.update_by
FROM bak_tk202610230007_material b
WHERE m.id = b.id;

COMMIT;

-- เมื่อยืนยันแล้วว่าไม่ต้อง rollback ค่อยลบตารางสำรอง
-- DROP TABLE bak_tk202610230007_sku, bak_tk202610230007_receipt_item, bak_tk202610230007_material;
