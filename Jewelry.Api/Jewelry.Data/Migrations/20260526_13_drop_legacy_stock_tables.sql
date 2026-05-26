-- =============================================
-- Migration: Drop legacy stock tables (Phase E — final retire)
-- Date: 2026-05-26
-- Description: ลบตารางเก่าทั้งหมดออกหลัง migrate + verify ใหม่ผ่าน E2E
--              ข้อมูลถูกย้ายไป tbt_stock_piece / _material / _cost_version / _cost_plan แล้ว
--              tbt_stock_product_image เป็น orphan test data — ไม่ใช้
-- Run order: หลัง all services migrate off legacy + E2E verified
-- Re-run safety: DROP TABLE IF EXISTS — idempotent
-- WARNING: ทำลายข้อมูล! ก่อนรันต้องมี backup snapshot
-- =============================================

-- ลำดับ drop ตาม FK chain:
-- 1. cost_plan → cost_version
-- 2. cost_version → stock_product
-- 3. material → stock_product
-- 4. image (no FK)
-- 5. stock_product

DROP TABLE IF EXISTS tbt_stock_cost_plan;
DROP TABLE IF EXISTS tbt_stock_cost_version;
DROP TABLE IF EXISTS tbt_stock_product_material;
DROP TABLE IF EXISTS tbt_stock_product_image;
DROP TABLE IF EXISTS tbt_stock_product;
