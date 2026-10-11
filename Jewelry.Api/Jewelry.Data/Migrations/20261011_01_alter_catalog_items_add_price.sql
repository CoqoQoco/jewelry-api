-- =============================================
-- Migration: Add selling price / sale price / currency to catalog items
-- Date: 2026-10-11
-- Ticket: TK202610070001
-- Description: ราคาขายเต็ม (price_full) + ราคาหลังลด (price_sale) + สกุลเงิน (price_currency)
--   ใช้พิมพ์ลง Catalog PDF — เป็นราคาขายให้ลูกค้า ไม่ใช่ต้นทุน stock
--   price_currency: 'THB' | 'USD'
--   ต้อง run SQL นี้ "ก่อน" deploy API
-- Re-run safety: Idempotent — ADD COLUMN IF NOT EXISTS
-- =============================================

ALTER TABLE tbt_catalog_product
    ADD COLUMN IF NOT EXISTS price_full     NUMERIC(18,2),
    ADD COLUMN IF NOT EXISTS price_sale     NUMERIC(18,2),
    ADD COLUMN IF NOT EXISTS price_currency CHARACTER VARYING(3) NOT NULL DEFAULT 'THB';

ALTER TABLE tbt_sale_document_catalog_item
    ADD COLUMN IF NOT EXISTS price_full     NUMERIC(18,2),
    ADD COLUMN IF NOT EXISTS price_sale     NUMERIC(18,2),
    ADD COLUMN IF NOT EXISTS price_currency CHARACTER VARYING(3) NOT NULL DEFAULT 'THB';
