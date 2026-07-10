-- =============================================
-- Migration: Add per-unit resize price and support columns to tbt_sale_billing_note_header
-- Date: 2026-07-10
-- Description: Add gold/silver resize per-unit price and support (subsidy) percent/amount columns
-- =============================================

ALTER TABLE tbt_sale_billing_note_header
    ADD COLUMN IF NOT EXISTS gold_resize_per_unit NUMERIC NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS silver_resize_per_unit NUMERIC NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS support_percent NUMERIC NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS support_amount NUMERIC NOT NULL DEFAULT 0;
