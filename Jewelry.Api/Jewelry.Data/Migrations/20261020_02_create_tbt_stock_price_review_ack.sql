-- =============================================
-- Migration: Price review ack (รับทราบราคาที่ควรตรวจ) per stock piece
-- Date: 2026-10-20
-- Description: เก็บการกด "ตรวจแล้ว" ของหน้า ราคาที่ควรตรวจ ต่อชิ้นสินค้า
--   price_acked = tbt_sku.default_price ณ ตอนกดรับทราบ (snapshot)
--   ack ที่ใช้งาน = แถวล่าสุดที่ is_active = true ของ stock_number นั้น
--   ถ้าราคาปัจจุบันไม่เท่า price_acked = ราคาเปลี่ยนหลังรับทราบ (กลับมาแสดงในรายการ)
--   is_active = false -> ยกเลิกการรับทราบ (เก็บประวัติ)
-- Re-run safety: Idempotent — CREATE TABLE/INDEX IF NOT EXISTS
-- =============================================

CREATE TABLE IF NOT EXISTS tbt_stock_price_review_ack (
    id              BIGSERIAL NOT NULL,
    stock_number    CHARACTER VARYING NOT NULL,
    price_acked     NUMERIC NOT NULL,
    note            CHARACTER VARYING(500),
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    create_by       CHARACTER VARYING NOT NULL,
    create_date     TIMESTAMPTZ NOT NULL,
    update_by       CHARACTER VARYING,
    update_date     TIMESTAMPTZ,
    CONSTRAINT tbt_stock_price_review_ack_pk PRIMARY KEY (id)
);

CREATE INDEX IF NOT EXISTS idx_tbt_stock_price_review_ack_stock
    ON tbt_stock_price_review_ack (stock_number, is_active);
