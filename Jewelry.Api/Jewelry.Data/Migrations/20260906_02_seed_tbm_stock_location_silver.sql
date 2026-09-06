-- =============================================
-- Migration: Seed tbm_stock_location — STNEW-SILVER
-- Date: 2026-09-06
-- Description: คลังปลายทางของงานเงิน batch 5-9-2026
--              ค่าฟิลด์อิงตามแถว STNEW / STNEW9K ที่มีอยู่จริง
--              (type=WAREHOUSE, parent_code=NULL, sort_order=NULL, is_sales_point=false)
-- =============================================

INSERT INTO tbm_stock_location
    (code, name_th, name_en, type, parent_code, is_sales_point, is_active, sort_order, create_date, create_by, is_temporary)
VALUES
    ('STNEW-SILVER', 'STOCK SILVER-NEW', 'STOCK SILVER-NEW', 'WAREHOUSE', NULL, FALSE, TRUE, NULL, now(), 'MIGRATION', FALSE)
ON CONFLICT (code) DO NOTHING;
