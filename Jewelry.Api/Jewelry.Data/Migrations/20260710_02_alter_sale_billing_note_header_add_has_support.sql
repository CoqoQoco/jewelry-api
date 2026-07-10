-- =============================================
-- Migration: Add has_support column to tbt_sale_billing_note_header
-- Date: 2026-07-10
-- Description: Add boolean flag to choose whether to pay support (subsidy) or not
-- =============================================

ALTER TABLE tbt_sale_billing_note_header
    ADD COLUMN IF NOT EXISTS has_support BOOLEAN NOT NULL DEFAULT false;
