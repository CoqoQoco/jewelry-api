-- =============================================
-- Migration: Add is_temporary to tbm_stock_location
-- Date: 2026-06-02
-- Description: เพิ่ม column is_temporary สำหรับระบุว่า location นี้เป็นชั่วคราว
-- =============================================

ALTER TABLE tbm_stock_location ADD COLUMN IF NOT EXISTS is_temporary BOOLEAN NOT NULL DEFAULT FALSE;

CREATE INDEX IF NOT EXISTS idx_location_is_temporary ON tbm_stock_location(is_temporary);
