-- =============================================
-- Migration: Create tbt_stock_piece_cost_plan
-- Date: 2026-05-26
-- Description: Phase A.4 — cost plan per piece
--              replaces legacy tbt_stock_cost_plan
--              fields อ้างอิงจาก TbtStockCostPlan entity
--              composite FK → tbt_stock_piece(stock_number, product_code)
--              version_running FK → tbt_stock_piece_cost_version(running) (nullable)
-- Run order: หลัง create_tbt_stock_piece_cost_version.sql
-- Re-run safety: CREATE TABLE IF NOT EXISTS + CREATE INDEX IF NOT EXISTS — idempotent
-- =============================================

CREATE TABLE IF NOT EXISTS tbt_stock_piece_cost_plan (
    running                 CHARACTER VARYING NOT NULL,
    stock_number            CHARACTER VARYING NOT NULL,
    product_code            CHARACTER VARYING NOT NULL,
    stock_number_origin     CHARACTER VARYING,
    version_running         CHARACTER VARYING,
    status_id               INT NOT NULL DEFAULT 0,
    status_name             CHARACTER VARYING NOT NULL,
    is_mobile_active        BOOLEAN,
    is_active               BOOLEAN,
    remark                  CHARACTER VARYING,
    create_date             TIMESTAMPTZ NOT NULL,
    create_by               CHARACTER VARYING NOT NULL,
    update_date             TIMESTAMPTZ,
    update_by               CHARACTER VARYING,
    CONSTRAINT tbt_stock_piece_cost_plan_pk PRIMARY KEY (running),
    CONSTRAINT tbt_stock_piece_cost_plan_piece_fk
        FOREIGN KEY (stock_number, product_code)
        REFERENCES tbt_stock_piece(stock_number, product_code),
    CONSTRAINT tbt_stock_piece_cost_plan_version_fk
        FOREIGN KEY (version_running)
        REFERENCES tbt_stock_piece_cost_version(running)
);

CREATE INDEX IF NOT EXISTS idx_piece_cost_plan_piece
    ON tbt_stock_piece_cost_plan(stock_number, product_code);

CREATE INDEX IF NOT EXISTS idx_piece_cost_plan_version
    ON tbt_stock_piece_cost_plan(version_running);
