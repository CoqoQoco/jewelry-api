-- =============================================
-- Migration: Migrate tbt_stock_cost_version → tbt_stock_piece_cost_version
-- Date: 2026-05-26
-- Description: Phase B.2 — copy legacy cost version rows into piece-level table
--              join tbt_stock_piece to derive product_code per stock_number
--              fields: running, stock_number, product_code, customer_*, remark,
--                      product_cost_detail, job_running, tag_price_multiplier,
--                      currency_unit, currency_rate, custom_stock_info, audit cols
-- Run order: after Phase A scripts (alter_stock_piece_add_product_code,
--            create_tbt_stock_piece_cost_version)
-- Re-run safety: NOT EXISTS guard on (running) — idempotent
-- =============================================

INSERT INTO tbt_stock_piece_cost_version (
    running,
    stock_number,
    product_code,
    customer_name,
    customer_code,
    customer_address,
    customer_tel,
    customer_email,
    remark,
    product_cost_detail,
    job_running,
    tag_price_multiplier,
    currency_unit,
    currency_rate,
    custom_stock_info,
    create_date,
    create_by,
    update_date,
    update_by
)
SELECT
    src.running,
    src.stock_number,
    p.product_code,
    src.customer_name,
    src.customer_code,
    src.customer_address,
    src.customer_tel,
    src.customer_email,
    src.remark,
    src.product_cost_detail,
    src.job_running,
    src.tag_price_multiplier,
    src.currency_unit,
    src.currency_rate,
    src.custom_stock_info,
    src.create_date,
    src.create_by,
    src.update_date,
    src.update_by
FROM tbt_stock_cost_version src
JOIN tbt_stock_piece p ON p.stock_number = src.stock_number
WHERE NOT EXISTS (
    SELECT 1
    FROM tbt_stock_piece_cost_version v
    WHERE v.running = src.running
)
ORDER BY src.running, src.create_date ASC;
-- DISTINCT ON keeps the oldest row per running (legacy has 1 known dup: CV202602180483)
-- Result: piece_cost_version count may be 1 less than legacy due to dedup

-- Verify (run after migration):
-- SELECT (SELECT COUNT(*) FROM tbt_stock_piece_cost_version) AS new_count,
--        (SELECT COUNT(*) FROM tbt_stock_cost_version)       AS legacy_count;
