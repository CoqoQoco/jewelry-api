-- =============================================
-- Migration: Create tbt_export_shipment tables
-- Date: 2026-08-18
-- Description: สร้างตาราง tbt_export_shipment / tbt_export_shipment_item
--   สำหรับออกเอกสารนำส่งสินค้าไปงานแฟร์ต่างประเทศ (consignment)
-- =============================================

-- =============================================
-- 1. tbt_export_shipment
-- =============================================
CREATE TABLE IF NOT EXISTS tbt_export_shipment (
    running             VARCHAR(50)   NOT NULL,
    document_number     VARCHAR(50)   NOT NULL,
    custom_number       VARCHAR(50),
    document_date       TIMESTAMPTZ   NOT NULL,
    consignee_name      VARCHAR(500),
    consignee_address   VARCHAR(1000),
    event_name          VARCHAR(500),
    booth_no            VARCHAR(100),
    attn_name           VARCHAR(200),
    attn_passport       VARCHAR(100),
    attn_tel            VARCHAR(100),
    incoterm            VARCHAR(100)  DEFAULT 'F.O.B. Bangkok',
    origin_country      VARCHAR(100)  DEFAULT 'THAILAND',
    currency            VARCHAR(10)   DEFAULT 'USD',
    exchange_rate       NUMERIC(18,4),
    price_percent       NUMERIC(18,4) DEFAULT 100,
    parcel_count        INT           DEFAULT 1,
    remark              VARCHAR(1000),
    status              INT           NOT NULL DEFAULT 0,
    -- Status: 0=Draft
    status_name         VARCHAR(50),
    is_active           BOOLEAN       NOT NULL DEFAULT TRUE,
    create_by           VARCHAR(100)  NOT NULL,
    create_date         TIMESTAMPTZ   NOT NULL,
    update_by           VARCHAR(100),
    update_date         TIMESTAMPTZ,
    CONSTRAINT tbt_export_shipment_pk PRIMARY KEY (running)
);

CREATE INDEX IF NOT EXISTS idx_export_shipment_document_number ON tbt_export_shipment (document_number);
CREATE INDEX IF NOT EXISTS idx_export_shipment_active          ON tbt_export_shipment (is_active);

-- =============================================
-- 2. tbt_export_shipment_item
-- =============================================
CREATE TABLE IF NOT EXISTS tbt_export_shipment_item (
    id                  BIGSERIAL     NOT NULL,
    shipment_running    VARCHAR(50)   NOT NULL,
    item_no             INT           NOT NULL,
    sort_order          INT           NOT NULL DEFAULT 0,
    stock_number        VARCHAR(100)  NOT NULL,
    product_code        VARCHAR(100),
    product_number      VARCHAR(100),
    description         VARCHAR(500),
    gold_weight         NUMERIC(18,4),
    stone_weight        NUMERIC(18,4),
    diamond_weight      NUMERIC(18,4),
    net_weight          NUMERIC(18,4),
    qty                 NUMERIC(18,2) DEFAULT 1,
    tag_price           NUMERIC(18,2),
    unit_price          NUMERIC(18,2),
    amount              NUMERIC(18,2),
    image_path          VARCHAR(1000),
    parcel_no           INT           DEFAULT 1,
    create_by           VARCHAR(100)  NOT NULL,
    create_date         TIMESTAMPTZ   NOT NULL,
    update_by           VARCHAR(100),
    update_date         TIMESTAMPTZ,
    CONSTRAINT tbt_export_shipment_item_pk PRIMARY KEY (id),
    CONSTRAINT tbt_export_shipment_item_shipment_fk FOREIGN KEY (shipment_running)
        REFERENCES tbt_export_shipment (running) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_export_shipment_item_shipment_running ON tbt_export_shipment_item (shipment_running);
CREATE UNIQUE INDEX IF NOT EXISTS tbt_export_shipment_item_shipment_stock_uq ON tbt_export_shipment_item (shipment_running, stock_number);
