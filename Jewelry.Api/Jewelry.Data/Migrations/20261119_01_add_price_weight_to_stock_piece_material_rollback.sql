-- TK202611190003 rollback
-- ต้อง revert API กลับเวอร์ชันก่อนหน้าก่อนรันไฟล์นี้ (EF map คอลัมน์ price_weight — ถ้า drop ขณะ API ใหม่ยังรันอยู่ query วัสดุจะพัง)
ALTER TABLE tbt_stock_piece_material DROP COLUMN IF EXISTS price_weight;
