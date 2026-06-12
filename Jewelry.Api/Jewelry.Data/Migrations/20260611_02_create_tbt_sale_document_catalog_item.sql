-- =============================================
-- Migration: Sale Document Catalog Item
-- Date: 2026-06-11
-- Description: สร้างตาราง item (detail) ของ Lookbook catalog
-- =============================================

-- =============================================
-- 2. tbt_sale_document_catalog_item (detail)
-- =============================================
CREATE TABLE IF NOT EXISTS tbt_sale_document_catalog_item (
    id                  BIGSERIAL NOT NULL,
    catalog_id          BIGINT NOT NULL,
    product_number      CHARACTER VARYING,
    description_line1   CHARACTER VARYING,
    description_line2   CHARACTER VARYING,
    dimension1          CHARACTER VARYING,
    dimension2          CHARACTER VARYING,
    dimension3          CHARACTER VARYING,
    sort_order          INT NOT NULL DEFAULT 0,
    CONSTRAINT tbt_sale_document_catalog_item_pk PRIMARY KEY (id),
    CONSTRAINT tbt_sale_document_catalog_item_catalog_id_fk FOREIGN KEY (catalog_id)
        REFERENCES tbt_sale_document_catalog (id)
);
