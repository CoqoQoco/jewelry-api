-- =============================================
-- Migration: Sale Document Catalog (Lookbook Builder)
-- Date: 2026-06-11
-- Description: สร้างตารางสำหรับ Product Lookbook builder
-- =============================================

-- =============================================
-- 1. tbt_sale_document_catalog (header)
-- =============================================
CREATE TABLE IF NOT EXISTS tbt_sale_document_catalog (
    id                  BIGSERIAL NOT NULL,
    header_label        CHARACTER VARYING,
    collection_title    CHARACTER VARYING,
    document_month      INT,
    document_year       INT,
    tags                CHARACTER VARYING,
    remark              CHARACTER VARYING,
    status              INT NOT NULL DEFAULT 0,
    -- Status: 0=Draft, 1=Final
    status_name         CHARACTER VARYING,
    is_active           BOOLEAN NOT NULL DEFAULT true,
    create_by           CHARACTER VARYING NOT NULL,
    create_date         TIMESTAMPTZ NOT NULL,
    update_by           CHARACTER VARYING,
    update_date         TIMESTAMPTZ,
    CONSTRAINT tbt_sale_document_catalog_pk PRIMARY KEY (id)
);
