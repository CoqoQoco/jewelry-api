-- =============================================
-- Migration: Create tbt_stock_piece_material
-- Date: 2026-05-26
-- Description: Phase A.4 — BOM per piece (replaces legacy tbt_stock_product_material)
--              composite FK → tbt_stock_piece(stock_number, product_code)
--              fields อ้างอิงจาก TbtStockProductMaterial entity
-- Run order: หลัง alter_stock_piece_add_product_code.sql
-- Re-run safety: CREATE TABLE IF NOT EXISTS + CREATE INDEX IF NOT EXISTS — idempotent
-- =============================================

CREATE TABLE IF NOT EXISTS tbt_stock_piece_material (
    id              BIGSERIAL NOT NULL,
    stock_number    CHARACTER VARYING NOT NULL,
    product_code    CHARACTER VARYING NOT NULL,
    type            CHARACTER VARYING NOT NULL,
    -- Type: Gold, Diamond, Gem, Setting, Worker, ETC
    type_name       CHARACTER VARYING,
    type_code       CHARACTER VARYING,
    type_barcode    CHARACTER VARYING,
    type_origin     CHARACTER VARYING,
    size            CHARACTER VARYING,
    qty             NUMERIC(18,4),
    qty_unit        CHARACTER VARYING,
    weight          NUMERIC(18,4),
    weight_unit     CHARACTER VARYING,
    region          CHARACTER VARYING,
    price           NUMERIC(18,2),
    create_date     TIMESTAMPTZ NOT NULL,
    create_by       CHARACTER VARYING NOT NULL,
    update_date     TIMESTAMPTZ,
    update_by       CHARACTER VARYING,
    CONSTRAINT tbt_stock_piece_material_pk PRIMARY KEY (id),
    CONSTRAINT tbt_stock_piece_material_piece_fk
        FOREIGN KEY (stock_number, product_code)
        REFERENCES tbt_stock_piece(stock_number, product_code)
);

CREATE INDEX IF NOT EXISTS idx_piece_material_piece
    ON tbt_stock_piece_material(stock_number, product_code);

CREATE INDEX IF NOT EXISTS idx_piece_material_type
    ON tbt_stock_piece_material(type);
