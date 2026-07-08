-- =============================================
-- Migration: Billing Note (ใบวางบิล)
-- Date: 2026-07-07
-- Description: Create tables for Billing Note feature (header, invoice items, product breakdown)
-- =============================================

-- =============================================
-- 1. tbt_sale_billing_note_header
-- =============================================
CREATE TABLE IF NOT EXISTS tbt_sale_billing_note_header (
    running                 CHARACTER VARYING NOT NULL,
    document_date           TIMESTAMPTZ NOT NULL,

    customer_code           CHARACTER VARYING NOT NULL,
    customer_name           CHARACTER VARYING NOT NULL,
    customer_address        CHARACTER VARYING,
    customer_tel            CHARACTER VARYING,

    bill_count              INT NOT NULL DEFAULT 0,

    gold_resize_qty         INT NOT NULL DEFAULT 0,
    gold_resize_amount      NUMERIC NOT NULL DEFAULT 0,
    silver_resize_qty       INT NOT NULL DEFAULT 0,
    silver_resize_amount    NUMERIC NOT NULL DEFAULT 0,

    sub_total               NUMERIC NOT NULL DEFAULT 0,
    vat_percent             NUMERIC NOT NULL DEFAULT 0,
    vat_amount              NUMERIC NOT NULL DEFAULT 0,
    grand_total             NUMERIC NOT NULL DEFAULT 0,

    remark                  CHARACTER VARYING,

    status                  INT NOT NULL DEFAULT 0,
    -- Status: 0=Active
    status_name             CHARACTER VARYING,

    is_delete               BOOLEAN NOT NULL DEFAULT FALSE,
    delete_reason           CHARACTER VARYING,

    create_by               CHARACTER VARYING NOT NULL,
    create_date             TIMESTAMPTZ NOT NULL,
    update_by               CHARACTER VARYING,
    update_date             TIMESTAMPTZ,

    CONSTRAINT tbt_sale_billing_note_header_pk PRIMARY KEY (running)
);

CREATE INDEX IF NOT EXISTS idx_tbt_sale_billing_note_header_customer_code ON tbt_sale_billing_note_header(customer_code);

-- =============================================
-- 2. tbt_sale_billing_note_item
-- =============================================
CREATE TABLE IF NOT EXISTS tbt_sale_billing_note_item (
    id                      BIGSERIAL NOT NULL,
    billing_note_running    CHARACTER VARYING NOT NULL,
    seq                     INT NOT NULL,
    invoice_running         CHARACTER VARYING NOT NULL,
    invoice_date            TIMESTAMPTZ,
    amount_before_vat       NUMERIC NOT NULL DEFAULT 0,
    remark                  CHARACTER VARYING,

    create_by               CHARACTER VARYING NOT NULL,
    create_date             TIMESTAMPTZ NOT NULL,
    update_by               CHARACTER VARYING,
    update_date             TIMESTAMPTZ,

    CONSTRAINT tbt_sale_billing_note_item_pk PRIMARY KEY (id),
    CONSTRAINT tbt_sale_billing_note_item_header_fk FOREIGN KEY (billing_note_running)
        REFERENCES tbt_sale_billing_note_header(running)
);

CREATE INDEX IF NOT EXISTS idx_tbt_sale_billing_note_item_header ON tbt_sale_billing_note_item(billing_note_running);
CREATE INDEX IF NOT EXISTS idx_tbt_sale_billing_note_item_invoice ON tbt_sale_billing_note_item(invoice_running);

-- =============================================
-- 3. tbt_sale_billing_note_product
-- =============================================
CREATE TABLE IF NOT EXISTS tbt_sale_billing_note_product (
    id                      BIGSERIAL NOT NULL,
    billing_note_running    CHARACTER VARYING NOT NULL,
    invoice_running         CHARACTER VARYING NOT NULL,
    product_number          CHARACTER VARYING,
    product_type            CHARACTER VARYING,
    product_type_name       CHARACTER VARYING,
    production_type         CHARACTER VARYING,
    qty                     NUMERIC NOT NULL DEFAULT 0,
    amount                  NUMERIC NOT NULL DEFAULT 0,
    remark                  CHARACTER VARYING,

    create_by               CHARACTER VARYING NOT NULL,
    create_date             TIMESTAMPTZ NOT NULL,
    update_by               CHARACTER VARYING,
    update_date             TIMESTAMPTZ,

    CONSTRAINT tbt_sale_billing_note_product_pk PRIMARY KEY (id),
    CONSTRAINT tbt_sale_billing_note_product_header_fk FOREIGN KEY (billing_note_running)
        REFERENCES tbt_sale_billing_note_header(running)
);

CREATE INDEX IF NOT EXISTS idx_tbt_sale_billing_note_product_header ON tbt_sale_billing_note_product(billing_note_running);
CREATE INDEX IF NOT EXISTS idx_tbt_sale_billing_note_product_invoice ON tbt_sale_billing_note_product(invoice_running);
