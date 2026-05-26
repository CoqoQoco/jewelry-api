-- =============================================
-- Migration: Create tbt_stock_piece_cost_version
-- Date: 2026-05-26
-- Description: Phase A.4 — cost version history per piece
--              replaces legacy tbt_stock_cost_version
--              fields อ้างอิงจาก TbtStockCostVersion entity
--              composite FK → tbt_stock_piece(stock_number, product_code)
-- Run order: หลัง alter_stock_piece_add_product_code.sql
-- Re-run safety: CREATE TABLE IF NOT EXISTS + CREATE INDEX IF NOT EXISTS — idempotent
-- =============================================

CREATE TABLE IF NOT EXISTS tbt_stock_piece_cost_version (
    running                 CHARACTER VARYING NOT NULL,
    stock_number            CHARACTER VARYING NOT NULL,
    product_code            CHARACTER VARYING NOT NULL,
    customer_name           CHARACTER VARYING,
    customer_code           CHARACTER VARYING,
    customer_address        CHARACTER VARYING,
    customer_tel            CHARACTER VARYING,
    customer_email          CHARACTER VARYING,
    remark                  CHARACTER VARYING,
    product_cost_detail     TEXT NOT NULL,
    job_running             CHARACTER VARYING,
    tag_price_multiplier    NUMERIC(18,4),
    currency_unit           CHARACTER VARYING,
    currency_rate           NUMERIC(18,6),
    custom_stock_info       CHARACTER VARYING,
    create_date             TIMESTAMPTZ NOT NULL,
    create_by               CHARACTER VARYING NOT NULL,
    update_date             TIMESTAMPTZ,
    update_by               CHARACTER VARYING,
    CONSTRAINT tbt_stock_piece_cost_version_pk PRIMARY KEY (running),
    CONSTRAINT tbt_stock_piece_cost_version_piece_fk
        FOREIGN KEY (stock_number, product_code)
        REFERENCES tbt_stock_piece(stock_number, product_code)
);

CREATE INDEX IF NOT EXISTS idx_piece_cost_version_piece
    ON tbt_stock_piece_cost_version(stock_number, product_code);
