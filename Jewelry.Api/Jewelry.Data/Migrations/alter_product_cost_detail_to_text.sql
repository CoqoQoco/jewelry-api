-- =============================================
-- Migration: Alter product_cost_detail jsonb -> text
-- Date: 2026-04-24
-- Description: Sync DB column type กับ EF mapping
--              JewelryContextExtensions.cs override เป็น text อยู่แล้ว
--              เพื่อรองรับแถว invalid JSON ตอนอ่าน
--              แต่ DB column จริงเป็น jsonb ทำให้ INSERT/UPDATE fail
--              ("column is of type jsonb but expression is of type text")
--
-- Affected tables:
--   tbt_stock_product.product_cost_detail
--   tbt_stock_cost_version.product_cost_detail
-- =============================================

BEGIN;

ALTER TABLE tbt_stock_product
    ALTER COLUMN product_cost_detail TYPE text
    USING product_cost_detail::text;

ALTER TABLE tbt_stock_cost_version
    ALTER COLUMN product_cost_detail TYPE text
    USING product_cost_detail::text;

COMMIT;
