-- =============================================
-- Migration: SKU Catalog Master (Phase 1 - Stock Refactor Plan D)
-- Date: 2026-05-25
-- Description: สร้าง table tbt_sku สำหรับ catalog master ของสินค้า
-- =============================================

-- =============================================
-- 1. tbt_sku
-- =============================================
CREATE TABLE IF NOT EXISTS tbt_sku (
    sku_code                CHARACTER VARYING NOT NULL,
    product_number          CHARACTER VARYING,
    product_name_th         CHARACTER VARYING NOT NULL,
    product_name_en         CHARACTER VARYING NOT NULL,
    product_type            CHARACTER VARYING,
    product_type_name       CHARACTER VARYING,
    mold                    CHARACTER VARYING,
    mold_design             CHARACTER VARYING,
    stud_earring            CHARACTER VARYING,
    size                    CHARACTER VARYING,
    production_type         CHARACTER VARYING,
    production_type_size    CHARACTER VARYING,
    image_name              CHARACTER VARYING,
    image_path              CHARACTER VARYING,
    default_price           NUMERIC(18,2),
    tag_price_multiplier    NUMERIC(10,4),
    is_active               BOOLEAN NOT NULL DEFAULT true,
    is_serialized           BOOLEAN NOT NULL DEFAULT true,
    remark                  CHARACTER VARYING,
    create_date             TIMESTAMPTZ NOT NULL,
    create_by               CHARACTER VARYING NOT NULL,
    update_date             TIMESTAMPTZ,
    update_by               CHARACTER VARYING,
    CONSTRAINT tbt_sku_pk PRIMARY KEY (sku_code)
);

CREATE INDEX IF NOT EXISTS idx_sku_product_number ON tbt_sku(product_number);
CREATE INDEX IF NOT EXISTS idx_sku_mold           ON tbt_sku(mold);
CREATE INDEX IF NOT EXISTS idx_sku_product_type   ON tbt_sku(product_type);
CREATE INDEX IF NOT EXISTS idx_sku_active         ON tbt_sku(is_active);

CREATE UNIQUE INDEX IF NOT EXISTS tbt_sku_product_number_uq ON tbt_sku(product_number)
    WHERE product_number IS NOT NULL;
