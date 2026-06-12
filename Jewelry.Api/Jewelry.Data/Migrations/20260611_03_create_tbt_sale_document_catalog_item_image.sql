-- =============================================
-- Migration: Sale Document Catalog Item Image
-- Date: 2026-06-11
-- Description: สร้างตารางเก็บรูปภาพของ item ใน Lookbook catalog
-- =============================================

-- =============================================
-- 3. tbt_sale_document_catalog_item_image
-- =============================================
CREATE TABLE IF NOT EXISTS tbt_sale_document_catalog_item_image (
    id          BIGSERIAL NOT NULL,
    item_id     BIGINT NOT NULL,
    blob_path   CHARACTER VARYING NOT NULL,
    sort_order  INT NOT NULL DEFAULT 0,
    CONSTRAINT tbt_sale_document_catalog_item_image_pk PRIMARY KEY (id),
    CONSTRAINT tbt_sale_document_catalog_item_image_item_id_fk FOREIGN KEY (item_id)
        REFERENCES tbt_sale_document_catalog_item (id)
);
