-- =============================================
-- Migration: Sale Material (ขายวัตถุดิบ / ขายพลอย)
-- Date: 2026-08-06
-- Description: ตารางเอกสารขายวัตถุดิบ (พลอย) จากคลังวัตถุดิบให้ลูกค้าภายนอก
-- =============================================

-- =============================================
-- 1. tbt_sale_material_header
-- =============================================
CREATE TABLE IF NOT EXISTS tbt_sale_material_header (
    running             CHARACTER VARYING NOT NULL,
    document_no         CHARACTER VARYING NOT NULL,
    document_date       TIMESTAMPTZ NOT NULL,

    customer_code       CHARACTER VARYING,
    customer_name       CHARACTER VARYING NOT NULL,
    customer_address    CHARACTER VARYING,
    customer_tel        CHARACTER VARYING,
    customer_email      CHARACTER VARYING,
    customer_tax_id     CHARACTER VARYING,

    sub_total           NUMERIC NOT NULL DEFAULT 0,
    vat_percent         NUMERIC NOT NULL DEFAULT 7,
    vat_amount          NUMERIC NOT NULL DEFAULT 0,
    grand_total         NUMERIC NOT NULL DEFAULT 0,

    remark              CHARACTER VARYING,

    status              INT NOT NULL DEFAULT 10,
    -- Status: 10=Draft, 100=Confirmed, 500=Cancelled
    status_name         CHARACTER VARYING,

    confirm_date        TIMESTAMPTZ,
    confirm_by          CHARACTER VARYING,
    cancel_date         TIMESTAMPTZ,
    cancel_by           CHARACTER VARYING,
    cancel_reason       CHARACTER VARYING,

    is_delete           BOOLEAN NOT NULL DEFAULT false,

    create_date         TIMESTAMPTZ NOT NULL,
    create_by           CHARACTER VARYING NOT NULL,
    update_date         TIMESTAMPTZ,
    update_by           CHARACTER VARYING,

    CONSTRAINT tbt_sale_material_header_pk PRIMARY KEY (running)
);

CREATE INDEX IF NOT EXISTS idx_tbt_sale_material_header_document_no ON tbt_sale_material_header(document_no);
CREATE INDEX IF NOT EXISTS idx_tbt_sale_material_header_customer_code ON tbt_sale_material_header(customer_code);
CREATE INDEX IF NOT EXISTS idx_tbt_sale_material_header_document_date ON tbt_sale_material_header(document_date);
CREATE INDEX IF NOT EXISTS idx_tbt_sale_material_header_status ON tbt_sale_material_header(status);

-- =============================================
-- 2. tbt_sale_material_item
-- =============================================
CREATE TABLE IF NOT EXISTS tbt_sale_material_item (
    id                  BIGSERIAL NOT NULL,
    running             CHARACTER VARYING NOT NULL,
    item_no             INT NOT NULL,

    gem_code            CHARACTER VARYING NOT NULL,
    gem_name            CHARACTER VARYING,
    gem_group           CHARACTER VARYING,
    gem_shape           CHARACTER VARYING,
    gem_size            CHARACTER VARYING,
    gem_grade           CHARACTER VARYING,
    description         CHARACTER VARYING,

    qty_piece           NUMERIC NOT NULL DEFAULT 0,
    qty_weight          NUMERIC NOT NULL DEFAULT 0,
    price_incl_vat      NUMERIC NOT NULL DEFAULT 0,
    price_excl_vat      NUMERIC NOT NULL DEFAULT 0,
    amount              NUMERIC NOT NULL DEFAULT 0,
    ref_stock_price     NUMERIC,

    remark              CHARACTER VARYING,

    CONSTRAINT tbt_sale_material_item_pk PRIMARY KEY (id),
    CONSTRAINT tbt_sale_material_item_running_fk FOREIGN KEY (running)
        REFERENCES tbt_sale_material_header (running)
);

CREATE INDEX IF NOT EXISTS idx_tbt_sale_material_item_running ON tbt_sale_material_item(running);
CREATE INDEX IF NOT EXISTS idx_tbt_sale_material_item_gem_code ON tbt_sale_material_item(gem_code);
