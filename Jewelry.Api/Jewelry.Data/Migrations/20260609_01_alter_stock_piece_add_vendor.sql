-- Migration: Add vendor to tbt_stock_piece
-- Date: 2026-06-09
ALTER TABLE tbt_stock_piece
    ADD COLUMN IF NOT EXISTS vendor CHARACTER VARYING;
