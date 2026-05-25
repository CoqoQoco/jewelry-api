-- =============================================
-- Migration: Backfill Stock Balance from Pieces (Phase 2 - Stock Refactor Plan D)
-- Date: 2026-05-25
-- Description: Aggregate tbt_stock_piece เข้า tbt_stock_balance per (sku_code, location_code)
--              qty_available เป็น GENERATED column — ไม่ต้อง insert
-- Run order: 3 (ต้องรัน backfill_piece ก่อน)
-- Re-run safe: ใช้ ON CONFLICT (sku_code, location_code) DO NOTHING — idempotent
--              หมายเหตุ: ถ้า balance row มีอยู่แล้ว script นี้จะไม่ update qty
--              ถ้าต้องการ recalculate ทั้งหมด ให้ TRUNCATE tbt_stock_balance ก่อนรัน
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
    COUNT(*) FILTER (WHERE status = 'IN_STOCK')     AS qty_on_hand,
    COUNT(*) FILTER (WHERE status = 'RESERVED')     AS qty_reserved,
    MAX(COALESCE(update_date, create_date))         AS last_movement_at,
    now()                                           AS create_date,
    'BACKFILL'                                      AS create_by
FROM tbt_stock_piece
GROUP BY sku_code, location_code
ON CONFLICT (sku_code, location_code) DO NOTHING;
