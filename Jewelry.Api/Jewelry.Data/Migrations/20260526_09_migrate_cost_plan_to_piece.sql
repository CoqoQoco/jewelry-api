-- =============================================
-- Migration: Migrate tbt_stock_cost_plan → tbt_stock_piece_cost_plan
-- Date: 2026-05-26
-- Description: Phase B.3 — copy legacy cost plan rows into piece-level table
--              join tbt_stock_piece to derive product_code per stock_number
--              fields: running, stock_number, product_code, stock_number_origin,
--                      version_running, status_id, status_name, is_mobile_active,
--                      is_active, remark, audit cols
--              NOTE: version_running FK references tbt_stock_piece_cost_version(running)
--                    run migrate_cost_version_to_piece.sql first so FK is satisfied
-- Run order: after Phase A scripts + after migrate_cost_version_to_piece.sql
-- Re-run safety: NOT EXISTS guard on (running) — idempotent
-- =============================================

INSERT INTO tbt_stock_piece_cost_plan (
    running,
    stock_number,
    product_code,
    stock_number_origin,
    version_running,
    status_id,
    status_name,
    is_mobile_active,
    is_active,
    remark,
    create_date,
    create_by,
    update_date,
    update_by
)
SELECT
    src.running,
    src.stock_number,
    p.product_code,
    src.stock_number_origin,
    src.version_running,
    src.status_id,
    src.status_name,
    src.is_mobile_active,
    src.is_active,
    src.remark,
    src.create_date,
    src.create_by,
    src.update_date,
    src.update_by
FROM tbt_stock_cost_plan src
JOIN tbt_stock_piece p ON p.stock_number = src.stock_number
WHERE NOT EXISTS (
    SELECT 1
    FROM tbt_stock_piece_cost_plan cp
    WHERE cp.running = src.running
);

-- Verify (run after migration):
-- SELECT (SELECT COUNT(*) FROM tbt_stock_piece_cost_plan) AS new_count,
--        (SELECT COUNT(*) FROM tbt_stock_cost_plan)       AS legacy_count;
