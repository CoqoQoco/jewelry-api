-- TK202611190003 : เก็บ "ราคา/น้ำหนัก" (QtyWeightPrice จากหน้ารับงานผลิต) ไว้กับวัสดุของสินค้า
-- รันก่อน deploy API
ALTER TABLE tbt_stock_piece_material ADD COLUMN IF NOT EXISTS price_weight numeric(18,2) NULL;
