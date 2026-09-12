-- =============================================
-- Migration: Add line_key to tbt_sale_order_product
-- Date: 2026-09-12
-- Description: รองรับหลายบรรทัดต่อเลขสินค้าเดียวกันในใบสั่งขาย (สแกนซ้ำเลขเดิมแบบไม่ติดกัน)
--              line_key เป็นรหัสประจำบรรทัดที่ฝั่งเว็บกำหนด ใช้แยกแต่ละบรรทัดที่เลขสินค้าซ้ำกัน
--              nullable เพราะแถวเดิมในระบบไม่มีค่านี้
-- =============================================
ALTER TABLE tbt_sale_order_product
    ADD COLUMN IF NOT EXISTS line_key CHARACTER VARYING(64);
