-- =============================================
-- Migration: Create catalog tables
-- Date: 2026-06-04
-- Description: สร้างตาราง tbm_product_catalog และ tbt_catalog_product
--              สำหรับระบบ Catalog จัดการ product catalog ที่แสดงต่อลูกค้า
-- =============================================

-- =============================================
-- 1. tbm_product_catalog (header)
-- =============================================
CREATE TABLE IF NOT EXISTS tbm_product_catalog (
    id                  SERIAL NOT NULL,
    code                CHARACTER VARYING NOT NULL,
    name_th             CHARACTER VARYING NOT NULL,
    name_en             CHARACTER VARYING,
    collection_title    CHARACTER VARYING,
    header_label        CHARACTER VARYING,
    is_active           BOOLEAN NOT NULL DEFAULT TRUE,
    create_date         TIMESTAMPTZ NOT NULL,
    create_by           CHARACTER VARYING NOT NULL,
    update_date         TIMESTAMPTZ,
    update_by           CHARACTER VARYING,
    CONSTRAINT tbm_product_catalog_pk PRIMARY KEY (id)
);

CREATE UNIQUE INDEX IF NOT EXISTS tbm_product_catalog_code_uq
    ON tbm_product_catalog(code);

-- =============================================
-- 2. tbt_catalog_product (items)
-- =============================================
CREATE TABLE IF NOT EXISTS tbt_catalog_product (
    id              BIGSERIAL NOT NULL,
    catalog_id      INT NOT NULL,
    product_number  CHARACTER VARYING NOT NULL,
    sort_order      INT NOT NULL DEFAULT 0,
    dimension_1     CHARACTER VARYING,
    dimension_2     CHARACTER VARYING,
    dimension_3     CHARACTER VARYING,
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    create_date     TIMESTAMPTZ NOT NULL,
    create_by       CHARACTER VARYING NOT NULL,
    update_date     TIMESTAMPTZ,
    update_by       CHARACTER VARYING,
    CONSTRAINT tbt_catalog_product_pk PRIMARY KEY (id),
    CONSTRAINT tbt_catalog_product_catalog_fk
        FOREIGN KEY (catalog_id)
        REFERENCES tbm_product_catalog(id)
);

CREATE INDEX IF NOT EXISTS idx_catalog_product_catalog_id
    ON tbt_catalog_product(catalog_id);
