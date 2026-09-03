-- =============================================
-- Migration: Create tbt_stock_gold
-- Date: 2026-08-31
-- Description: คลังทอง (Gold Stock) — ยอดคงเหลือแยกตาม gold_code + gold_size_code
--              เลียนแบบ pattern ของ tbt_stock_gem (พลอย) แต่แยกตารางใหม่
--              ไม่แตะ tbt_stock_gem / tbt_stock_gem_transection ที่ใช้งานอยู่จริง
-- Run order: ก่อน 20260831_02_create_tbt_stock_gold_transection.sql (แถวลูกอ้างอิง
--            gold_code + gold_size_code เดียวกัน)
-- Re-run safety: CREATE TABLE IF NOT EXISTS + CREATE INDEX IF NOT EXISTS — idempotent
-- =============================================

CREATE TABLE IF NOT EXISTS tbt_stock_gold (
    id                  BIGSERIAL NOT NULL,
    gold_code           CHARACTER VARYING NOT NULL,
    gold_size_code      CHARACTER VARYING NOT NULL,
    weight              NUMERIC NOT NULL DEFAULT 0,
    -- คงเหลือในคลัง (กรัม)
    weight_on_process   NUMERIC NOT NULL DEFAULT 0,
    -- เบิกออกแล้วยังไม่คืน
    create_date         TIMESTAMPTZ NOT NULL,
    create_by           CHARACTER VARYING NOT NULL,
    update_date         TIMESTAMPTZ,
    update_by           CHARACTER VARYING,
    CONSTRAINT tbt_stock_gold_pk PRIMARY KEY (id),
    CONSTRAINT tbt_stock_gold_gold_code_fk
        FOREIGN KEY (gold_code) REFERENCES tbm_gold(code),
    CONSTRAINT tbt_stock_gold_gold_size_code_fk
        FOREIGN KEY (gold_size_code) REFERENCES tbm_gold_size(code),
    CONSTRAINT tbt_stock_gold_code_size_uq UNIQUE (gold_code, gold_size_code)
);
-- หมายเหตุ: UNIQUE (gold_code, gold_size_code) สร้าง index ให้อัตโนมัติแล้ว ไม่ต้องเพิ่ม index ซ้ำ
