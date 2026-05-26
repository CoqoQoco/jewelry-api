-- =============================================
-- Migration: Drop tbt_sku_image (Option A — revert)
-- Date: 2026-05-26
-- Description: ตัดสินใจใช้ tbt_sku.image_name/image_path ตามเดิม
--              tbt_sku_image table ที่สร้างจาก script 06 ไม่ใช้แล้ว
--              legacy tbt_stock_product_image เป็น orphan test data (6 rows)
--              tbt_sku.image_name ครบ 4,506 SKUs จาก Phase 1 backfill — เพียงพอ
-- Run order: หลัง decision เปลี่ยนเป็น Option A
-- Re-run safety: DROP TABLE IF EXISTS — idempotent
-- =============================================

DROP TABLE IF EXISTS tbt_sku_image;
