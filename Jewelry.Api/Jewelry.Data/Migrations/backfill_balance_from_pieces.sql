-- =============================================
-- Migration: Backfill Stock Balance from Pieces (Phase 2 - Stock Refactor Plan D)
-- Date: 2026-05-25 (fixed 2026-05-26)
-- Description: Aggregate tbt_stock_piece เข้า tbt_stock_balance per (sku_code, location_code)
--              qty_on_hand นับ IN_STOCK + RESERVED (reserved ยังอยู่ในคลัง, แค่จอง)
--              qty_reserved นับ RESERVED เท่านั้น
--              qty_available เป็น GENERATED column — ไม่ต้อง insert
-- Run order: 3 (ต้องรัน backfill_piece ก่อน)
-- Re-run safe: ON CONFLICT DO UPDATE — overwrite ค่าเดิมด้วยการนับใหม่ทุก run
-- =============================================

INSERT INTO tbt_stock_balance (
    sku_code,
    location_code,
    qty_on_hand,
    qty_reserved,
    last_movement_at,
    create_date,
    create_by
)
SELECT
    sku_code,
    location_code,
    COUNT(*) FILTER (WHERE status IN ('IN_STOCK', 'RESERVED'))  AS qty_on_hand,
    COUNT(*) FILTER (WHERE status = 'RESERVED')                 AS qty_reserved,
    MAX(COALESCE(update_date, create_date))                     AS last_movement_at,
    now()                                                       AS create_date,
    'BACKFILL'                                                  AS create_by
FROM tbt_stock_piece
GROUP BY sku_code, location_code
ON CONFLICT (sku_code, location_code) DO UPDATE SET
    qty_on_hand       = EXCLUDED.qty_on_hand,
    qty_reserved      = EXCLUDED.qty_reserved,
    last_movement_at  = EXCLUDED.last_movement_at,
    update_date       = now(),
    update_by         = 'BACKFILL';
