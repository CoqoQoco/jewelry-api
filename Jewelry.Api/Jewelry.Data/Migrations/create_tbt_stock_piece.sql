-- =============================================
-- Migration: Stock Piece (Phase 1 - Stock Refactor Plan D)
-- Date: 2026-05-25
-- Description: สร้าง table tbt_stock_piece สำหรับ per-piece tracking
--              PK = stock_number ตรงกับ legacy tbt_stock_product.stock_number
-- =============================================

-- =============================================
-- 1. tbt_stock_piece
-- =============================================
CREATE TABLE IF NOT EXISTS tbt_stock_piece (
    stock_number        CHARACTER VARYING NOT NULL,
    sku_code            CHARACTER VARYING NOT NULL,
    location_code       CHARACTER VARYING NOT NULL,
    status              CHARACTER VARYING,
    -- Status: IN_STOCK, RESERVED, SOLD, TRANSIT, LOST, RETURNED
    receipt_number      CHARACTER VARYING,
    receipt_type        CHARACTER VARYING,
    receipt_date        TIMESTAMPTZ,
    production_date     TIMESTAMPTZ,
    wo                  CHARACTER VARYING,
    wo_number           INT,
    wo_origin           CHARACTER VARYING,
    po_number           CHARACTER VARYING,
    product_cost        NUMERIC(18,2),
    product_cost_detail TEXT,
    weight_actual       NUMERIC(18,4),
    size_actual         CHARACTER VARYING,
    barcode             CHARACTER VARYING,
    remark              CHARACTER VARYING,
    create_date         TIMESTAMPTZ NOT NULL,
    create_by           CHARACTER VARYING NOT NULL,
    update_date         TIMESTAMPTZ,
    update_by           CHARACTER VARYING,
    CONSTRAINT tbt_stock_piece_pk PRIMARY KEY (stock_number),
    CONSTRAINT tbt_stock_piece_sku_fk FOREIGN KEY (sku_code)
        REFERENCES tbt_sku(sku_code),
    CONSTRAINT tbt_stock_piece_location_fk FOREIGN KEY (location_code)
        REFERENCES tbm_stock_location(code)
);

CREATE INDEX IF NOT EXISTS idx_piece_sku      ON tbt_stock_piece(sku_code);
CREATE INDEX IF NOT EXISTS idx_piece_location ON tbt_stock_piece(location_code);
CREATE INDEX IF NOT EXISTS idx_piece_status   ON tbt_stock_piece(status);
CREATE INDEX IF NOT EXISTS idx_piece_wo       ON tbt_stock_piece(wo);
CREATE INDEX IF NOT EXISTS idx_piece_barcode  ON tbt_stock_piece(barcode);
