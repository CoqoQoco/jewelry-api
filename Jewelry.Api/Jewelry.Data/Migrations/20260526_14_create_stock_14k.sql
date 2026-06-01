-- =============================================
-- Migration: Create stock_14k table
-- Date: 2026-05-26
-- Description: Legacy import table สำหรับ 14K batch (mirror stock_9k)
-- Run order: หลัง Phase D — ใช้บน dev เท่านั้น (prod ใช้ backfill แทน)
-- =============================================

CREATE TABLE IF NOT EXISTS stock_14k (
    id          SERIAL,
    no_product  CHARACTER VARYING,
    style_no    CHARACTER VARYING,
    qty         INTEGER,
    is_transfer BOOLEAN NOT NULL DEFAULT FALSE,
    CONSTRAINT stock_14k_pk PRIMARY KEY (id)
);

CREATE INDEX IF NOT EXISTS idx_stock_14k_no_product ON stock_14k(no_product);
CREATE INDEX IF NOT EXISTS idx_stock_14k_is_transfer ON stock_14k(is_transfer);
