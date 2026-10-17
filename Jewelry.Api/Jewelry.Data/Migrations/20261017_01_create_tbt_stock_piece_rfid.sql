-- =============================================
-- Migration: RFID tag binding for stock pieces
-- Date: 2026-10-17
-- Description: ผูกแท็ก UHF RFID (EPC) กับชิ้นสินค้า tbt_stock_piece (คีย์ผสม stock_number + product_code)
--   แยกตารางเพื่อรองรับเปลี่ยนแท็ก/ผูกใหม่ โดยไม่กระทบคีย์ผสมเดิม
--   is_active = true  -> การผูกที่ใช้งานอยู่ (1 epc ต่อ 1 ชิ้น, 1 ชิ้นต่อ 1 epc)
--   is_active = false -> ยกเลิกการผูกแล้ว (เก็บประวัติ, ผูกใหม่ได้)
--   epc/tid เก็บตัวพิมพ์ใหญ่ ไม่มีช่องว่าง (API normalize ก่อนบันทึก)
-- Re-run safety: Idempotent — CREATE TABLE/INDEX IF NOT EXISTS, รันซ้ำได้ไม่ error
-- =============================================

CREATE TABLE IF NOT EXISTS tbt_stock_piece_rfid (
    id              BIGSERIAL NOT NULL,
    epc             CHARACTER VARYING NOT NULL,
    tid             CHARACTER VARYING,
    stock_number    CHARACTER VARYING NOT NULL,
    product_code    CHARACTER VARYING NOT NULL,
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    create_date     TIMESTAMPTZ NOT NULL,
    create_by       CHARACTER VARYING NOT NULL,
    update_date     TIMESTAMPTZ,
    update_by       CHARACTER VARYING,
    CONSTRAINT tbt_stock_piece_rfid_pk PRIMARY KEY (id)
);

CREATE UNIQUE INDEX IF NOT EXISTS idx_tbt_stock_piece_rfid_epc_active
    ON tbt_stock_piece_rfid (epc)
    WHERE is_active;

CREATE UNIQUE INDEX IF NOT EXISTS idx_tbt_stock_piece_rfid_piece_active
    ON tbt_stock_piece_rfid (stock_number, product_code)
    WHERE is_active;
