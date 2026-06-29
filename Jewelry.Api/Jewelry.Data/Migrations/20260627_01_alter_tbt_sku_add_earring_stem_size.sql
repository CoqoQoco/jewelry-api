-- Migration: Add earring_stem_size to tbt_sku
-- Date: 2026-06-27
ALTER TABLE tbt_sku
    ADD COLUMN IF NOT EXISTS earring_stem_size CHARACTER VARYING;
