-- Migration: Add tax_id to tbm_customer
-- Date: 2026-08-06
ALTER TABLE tbm_customer
    ADD COLUMN IF NOT EXISTS tax_id CHARACTER VARYING;
