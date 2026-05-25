-- =============================================
-- Migration: Stock Location Master (Phase 1 - Stock Refactor Plan D)
-- Date: 2026-05-25
-- Description: สร้าง table tbm_stock_location สำหรับ master location/warehouse
-- =============================================

-- =============================================
-- 1. tbm_stock_location
-- =============================================
CREATE TABLE IF NOT EXISTS tbm_stock_location (
    code            CHARACTER VARYING NOT NULL,
    name_th         CHARACTER VARYING NOT NULL,
    name_en         CHARACTER VARYING,
    type            CHARACTER VARYING,
    -- Type: WAREHOUSE, SHOWROOM, BRANCH, VAULT
    parent_code     CHARACTER VARYING,
    is_sales_point  BOOLEAN NOT NULL DEFAULT false,
    is_active       BOOLEAN NOT NULL DEFAULT true,
    sort_order      INT,
    create_date     TIMESTAMPTZ NOT NULL,
    create_by       CHARACTER VARYING NOT NULL,
    update_date     TIMESTAMPTZ,
    update_by       CHARACTER VARYING,
    CONSTRAINT tbm_stock_location_pk PRIMARY KEY (code),
    CONSTRAINT tbm_stock_location_parent_fk FOREIGN KEY (parent_code)
        REFERENCES tbm_stock_location(code)
);

CREATE INDEX IF NOT EXISTS idx_location_parent ON tbm_stock_location(parent_code);
CREATE INDEX IF NOT EXISTS idx_location_active ON tbm_stock_location(is_active);

-- =============================================
-- 2. Seed MAIN location
-- =============================================
INSERT INTO tbm_stock_location (code, name_th, name_en, type, is_sales_point, is_active, sort_order, create_date, create_by)
VALUES ('MAIN', 'คลังหลัก', 'Main Warehouse', 'WAREHOUSE', false, true, 1, now(), 'MIGRATION')
ON CONFLICT DO NOTHING;
