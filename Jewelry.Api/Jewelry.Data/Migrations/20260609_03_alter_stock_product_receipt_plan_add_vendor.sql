-- Migration: Add vendor to tbt_stock_product_receipt_plan
-- Date: 2026-06-09
ALTER TABLE tbt_stock_product_receipt_plan
    ADD COLUMN IF NOT EXISTS vendor CHARACTER VARYING;
