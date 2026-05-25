-- =============================================
-- Migration: Stock Balance (Phase 1 - Stock Refactor Plan D)
-- Date: 2026-05-25
-- Description: สร้าง table tbt_stock_balance สำหรับ aggregate balance per sku x location
-- =============================================

-- =============================================
-- 1. tbt_stock_balance
-- =============================================
CREATE TABLE IF NOT EXISTS tbt_stock_balance (
    id                  BIGSERIAL NOT NULL,
    sku_code            CHARACTER VARYING NOT NULL,
    location_code       CHARACTER VARYING NOT NULL,
    qty_on_hand         NUMERIC(18,4) NOT NULL DEFAULT 0,
    qty_reserved        NUMERIC(18,4) NOT NULL DEFAULT 0,
    qty_available       NUMERIC(18,4) GENERATED ALWAYS AS (qty_on_hand - qty_reserved) STORED,
    last_movement_at    TIMESTAMPTZ,
    create_date         TIMESTAMPTZ NOT NULL,
    create_by           CHARACTER VARYING NOT NULL,
    update_date         TIMESTAMPTZ,
    update_by           CHARACTER VARYING,
    CONSTRAINT tbt_stock_balance_pk PRIMARY KEY (id),
    CONSTRAINT tbt_stock_balance_sku_fk FOREIGN KEY (sku_code)
        REFERENCES tbt_sku(sku_code),
    CONSTRAINT tbt_stock_balance_location_fk FOREIGN KEY (location_code)
        REFERENCES tbm_stock_location(code)
);

CREATE UNIQUE INDEX IF NOT EXISTS tbt_stock_balance_sku_location_uq
    ON tbt_stock_balance(sku_code, location_code);

CREATE INDEX IF NOT EXISTS idx_balance_available
    ON tbt_stock_balance(sku_code, location_code)
    WHERE qty_available > 0;
