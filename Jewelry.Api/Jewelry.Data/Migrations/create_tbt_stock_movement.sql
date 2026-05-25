-- =============================================
-- Migration: Stock Movement Ledger (Phase 1 - Stock Refactor Plan D)
-- Date: 2026-05-25
-- Description: สร้าง table tbt_stock_movement สำหรับ immutable audit ledger
--              ห้ามแก้ไข row — หากแก้ผิดให้สร้าง reversing row แทน
-- =============================================

-- =============================================
-- 1. tbt_stock_movement
-- =============================================
CREATE TABLE IF NOT EXISTS tbt_stock_movement (
    id              BIGSERIAL NOT NULL,
    movement_date   TIMESTAMPTZ NOT NULL,
    movement_type   CHARACTER VARYING NOT NULL,
    -- Type: RECEIPT, SALE, RETURN, TRANSFER_OUT, TRANSFER_IN, ADJUST, RESERVE, UNRESERVE
    sku_code        CHARACTER VARYING NOT NULL,
    stock_number    CHARACTER VARYING,
    from_location   CHARACTER VARYING,
    to_location     CHARACTER VARYING,
    qty             NUMERIC(18,4) NOT NULL,
    ref_doc_type    CHARACTER VARYING,
    -- ref_doc_type: SO, WO, BASKET, RECEIPT, INVOICE, INVOICE_DELETE, ADJUST
    ref_doc_no      CHARACTER VARYING,
    remark          CHARACTER VARYING,
    create_date     TIMESTAMPTZ NOT NULL,
    create_by       CHARACTER VARYING NOT NULL,
    CONSTRAINT tbt_stock_movement_pk PRIMARY KEY (id),
    CONSTRAINT tbt_stock_movement_sku_fk FOREIGN KEY (sku_code)
        REFERENCES tbt_sku(sku_code),
    CONSTRAINT tbt_stock_movement_piece_fk FOREIGN KEY (stock_number)
        REFERENCES tbt_stock_piece(stock_number),
    CONSTRAINT tbt_stock_movement_from_location_fk FOREIGN KEY (from_location)
        REFERENCES tbm_stock_location(code),
    CONSTRAINT tbt_stock_movement_to_location_fk FOREIGN KEY (to_location)
        REFERENCES tbm_stock_location(code)
);

CREATE INDEX IF NOT EXISTS idx_movement_date  ON tbt_stock_movement(movement_date);
CREATE INDEX IF NOT EXISTS idx_movement_sku   ON tbt_stock_movement(sku_code);
CREATE INDEX IF NOT EXISTS idx_movement_piece ON tbt_stock_movement(stock_number);
CREATE INDEX IF NOT EXISTS idx_movement_ref   ON tbt_stock_movement(ref_doc_type, ref_doc_no);
