-- =============================================
-- Migration: Add indexes to tbt_stock_gem_transection (running, type+status, code)
-- Date: 2026-09-25
-- Description: ReceiptAndIssueStockGem/Picklist ทำ seq scan บนตาราง tbt_stock_gem_transection
--              (51k แถว, 22.6k running ต่างกัน) ทุกครั้งที่เรียก เพราะตารางไม่มี index อื่น
--              นอกจาก PK (id) — เพิ่ม index รองรับ filter ที่ endpoint นี้ใช้จริง (running
--              contains, type+status equality, join กับ tbt_stock_gem ผ่าน code)
-- Note: CREATE INDEX CONCURRENTLY ห้ามรันอยู่ใน transaction block (autocommit เท่านั้น)
--       รันได้ระหว่างใช้งานปกติโดยไม่ล็อกตาราง (ใช้เวลานานกว่า CREATE INDEX ธรรมดา)
-- =============================================

CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_tbt_stock_gem_transection_running ON public.tbt_stock_gem_transection (running);
CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_tbt_stock_gem_transection_type_status ON public.tbt_stock_gem_transection (type, stastus);
CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_tbt_stock_gem_transection_code ON public.tbt_stock_gem_transection (code);
