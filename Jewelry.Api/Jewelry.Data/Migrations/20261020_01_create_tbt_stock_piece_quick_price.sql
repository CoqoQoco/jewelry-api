-- =============================================
-- Migration: Quick Price (ตีราคาด่วน) history per stock piece
-- Date: 2026-10-20
-- Description: เก็บประวัติราคาตีด่วนต่อชิ้นสินค้า แยกจากการตีราคาปกติ (cost version/plan)
--   ไม่แตะ tbt_sku.default_price, tbt_stock_piece.product_cost
--   sale_price = tbt_sku.default_price ณ ตอนตี (snapshot ต่อชิ้น)
--   cost = sale_price / markup
--   cost_ex_gold = cost - gold_weight * gold_price
--   quick_price = cost_ex_gold * (1 + percent/100) แล้วปัดตาม rounding
--   rounding: 0 = ปัด 2 ตำแหน่ง (half-up), 10/100/1000 = ปัดขึ้นเป็นผลคูณ
--   is_active = true -> ใช้งาน, false -> ยกเลิก (เก็บประวัติ)
-- Re-run safety: Idempotent — CREATE TABLE/INDEX IF NOT EXISTS
-- =============================================

CREATE TABLE IF NOT EXISTS tbt_stock_piece_quick_price (
    id              BIGSERIAL NOT NULL,
    stock_number    CHARACTER VARYING NOT NULL,
    sale_price      NUMERIC NOT NULL,
    markup          NUMERIC NOT NULL,
    gold_weight     NUMERIC NOT NULL,
    gold_price      NUMERIC NOT NULL,
    cost            NUMERIC NOT NULL,
    cost_ex_gold    NUMERIC NOT NULL,
    percent         NUMERIC NOT NULL,
    rounding        INT NOT NULL,
    quick_price     NUMERIC NOT NULL,
    remark          CHARACTER VARYING(500),
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    create_by       CHARACTER VARYING NOT NULL,
    create_date     TIMESTAMPTZ NOT NULL,
    update_by       CHARACTER VARYING,
    update_date     TIMESTAMPTZ,
    CONSTRAINT tbt_stock_piece_quick_price_pk PRIMARY KEY (id)
);

CREATE INDEX IF NOT EXISTS idx_tbt_stock_piece_quick_price_stock
    ON tbt_stock_piece_quick_price (stock_number, create_date DESC);
