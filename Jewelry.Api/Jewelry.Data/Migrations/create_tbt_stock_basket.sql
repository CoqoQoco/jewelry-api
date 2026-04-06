-- =============================================
-- Migration: Stock Basket Feature (Phase 1)
-- Date: 2026-04-06
-- Description: สร้าง tables สำหรับ feature ตะกร้าสินค้า
-- =============================================

-- =============================================
-- 1. tbt_stock_basket (Header)
-- =============================================
CREATE TABLE IF NOT EXISTS tbt_stock_basket (
    running         CHARACTER VARYING NOT NULL,
    basket_number   CHARACTER VARYING NOT NULL,
    basket_name     CHARACTER VARYING NOT NULL,
    event_date      TIMESTAMPTZ,
    responsible     CHARACTER VARYING,
    status          INT NOT NULL DEFAULT 0,
    -- Status: 0=Draft, 1=PendingApproval, 2=Approved, 3=CheckedOut, 4=Closed
    status_name     CHARACTER VARYING,
    remark          CHARACTER VARYING,
    checkout_date   TIMESTAMPTZ,
    create_date     TIMESTAMPTZ NOT NULL,
    create_by       CHARACTER VARYING NOT NULL,
    update_date     TIMESTAMPTZ,
    update_by       CHARACTER VARYING,
    CONSTRAINT tbt_stock_basket_pk PRIMARY KEY (running)
);

-- =============================================
-- 2. tbt_stock_basket_item (Items)
-- =============================================
CREATE TABLE IF NOT EXISTS tbt_stock_basket_item (
    id              BIGSERIAL,
    basket_running  CHARACTER VARYING NOT NULL,
    basket_number   CHARACTER VARYING NOT NULL,
    stock_number    CHARACTER VARYING NOT NULL,
    -- Status: InBasket, Sold, Returned
    status          CHARACTER VARYING NOT NULL DEFAULT 'InBasket',
    status_name     CHARACTER VARYING,
    create_date     TIMESTAMPTZ NOT NULL,
    create_by       CHARACTER VARYING NOT NULL,
    update_date     TIMESTAMPTZ,
    update_by       CHARACTER VARYING,
    CONSTRAINT tbt_stock_basket_item_pk PRIMARY KEY (id),
    CONSTRAINT tbt_stock_basket_item_basket_fk FOREIGN KEY (basket_running)
        REFERENCES tbt_stock_basket(running)
);

CREATE INDEX IF NOT EXISTS idx_basket_item_stock  ON tbt_stock_basket_item(stock_number);
CREATE INDEX IF NOT EXISTS idx_basket_item_basket ON tbt_stock_basket_item(basket_running);
