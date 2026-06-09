-- Migration: Add totals columns to tbt_sale_quotation
-- Date: 2026-06-09
ALTER TABLE tbt_sale_quotation
    ADD COLUMN IF NOT EXISTS sub_total NUMERIC,
    ADD COLUMN IF NOT EXISTS special_discount_amt NUMERIC,
    ADD COLUMN IF NOT EXISTS special_addition_amt NUMERIC,
    ADD COLUMN IF NOT EXISTS freight_amt NUMERIC,
    ADD COLUMN IF NOT EXISTS vat_amount NUMERIC,
    ADD COLUMN IF NOT EXISTS grand_total_raw NUMERIC,
    ADD COLUMN IF NOT EXISTS grand_total_rounded NUMERIC,
    ADD COLUMN IF NOT EXISTS rounding_adjustment NUMERIC;
