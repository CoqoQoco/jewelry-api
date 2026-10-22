-- =============================================
-- Migration: Seed tbm_material_stock_policy (ค่าเริ่มต้นคลังวัตถุดิบ GOLD / GEM)
-- Date: 2026-10-22
-- Description: แถวนโยบายเริ่มต้นต่อประเภทวัตถุดิบ — ค่าตรงกับ default ใน API (MaterialInsightThresholds)
--   window_months 3, lead_time_days 14, safety_days 7, floor_qty 0, near_percent 125,
--   dead_stock_days 180, forecast_months 3, on_process_stale_days 30, min_history_months 3
--   price_change_percent 10, concentration_percent 50 (ใช้กับพลอยในอนาคต — GOLD เก็บค่าเดียวกันแต่ยังไม่ถูกใช้)
--   ต้อง run หลัง 20261022_01_create_material_stock_tables.sql
-- Re-run safety: Idempotent — ON CONFLICT DO NOTHING (ไม่ทับค่าที่ผู้ใช้แก้แล้ว)
-- =============================================

INSERT INTO tbm_material_stock_policy
    (material_kind, window_months, lead_time_days, safety_days, floor_qty, near_percent,
     dead_stock_days, forecast_months, on_process_stale_days, price_change_percent,
     concentration_percent, min_history_months, create_by, create_date)
VALUES
    ('GOLD', 3, 14, 7, 0, 125, 180, 3, 30, 10, 50, 3, 'system', NOW()),
    ('GEM',  3, 14, 7, 0, 125, 180, 3, 30, 10, 50, 3, 'system', NOW())
ON CONFLICT (material_kind) DO NOTHING;

-- =============================================
-- Verification (uncomment to run manually)
-- =============================================
-- SELECT * FROM tbm_material_stock_policy ORDER BY material_kind;
