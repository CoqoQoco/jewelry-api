-- =============================================
-- Migration: Create tbt_sku_image
-- Date: 2026-05-26
-- Description: Phase A.4 — image ย้ายเป็น catalog level (SKU level)
--              replaces legacy tbt_stock_product_image (ที่เดิม FK by stock_number)
--              fields อ้างอิงจาก TbtStockProductImage entity
--              เปลี่ยน FK จาก stock_number → sku_code (tbt_sku)
-- Run order: หลัง create_tbt_sku.sql (tbt_sku ต้องมีก่อน)
-- Re-run safety: CREATE TABLE IF NOT EXISTS + CREATE INDEX IF NOT EXISTS — idempotent
-- =============================================

CREATE TABLE IF NOT EXISTS tbt_sku_image (
    id              BIGSERIAL NOT NULL,
    sku_code        CHARACTER VARYING NOT NULL,
    name            CHARACTER VARYING NOT NULL,
    name_path       CHARACTER VARYING NOT NULL,
    -- name_path: Azure Blob storage path
    year            INT,
    sort_order      INT,
    is_active       BOOLEAN,
    remark          CHARACTER VARYING,
    create_date     TIMESTAMPTZ NOT NULL,
    create_by       CHARACTER VARYING NOT NULL,
    update_date     TIMESTAMPTZ,
    update_by       CHARACTER VARYING,
    CONSTRAINT tbt_sku_image_pk PRIMARY KEY (id),
    CONSTRAINT tbt_sku_image_sku_fk
        FOREIGN KEY (sku_code)
        REFERENCES tbt_sku(sku_code)
);

CREATE INDEX IF NOT EXISTS idx_sku_image_sku_code
    ON tbt_sku_image(sku_code);
