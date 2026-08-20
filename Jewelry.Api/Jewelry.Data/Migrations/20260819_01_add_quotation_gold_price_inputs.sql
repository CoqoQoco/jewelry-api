-- Migration: Add gold price input columns to tbt_sale_quotation
-- Ticket: TK202608180001
-- Date: 2026-08-19
-- Description: เก็บ input ราคาทองที่ใช้คำนวณ gold_per_oz (spot / premium / markup)
--              เพื่อให้ trace ย้อนหลังได้ และ prefill modal คำนวณราคาทองถูกต้อง
--              คอลัมน์ gold_per_oz เดิมยังใช้ชื่อเดิม ห้ามเปลี่ยน/ลบ
-- IMPORTANT: ต้องรัน migration นี้ก่อน deploy API ตัวใหม่

ALTER TABLE tbt_sale_quotation
    ADD COLUMN IF NOT EXISTS gold_spot_price NUMERIC;

ALTER TABLE tbt_sale_quotation
    ADD COLUMN IF NOT EXISTS gold_premium NUMERIC;

ALTER TABLE tbt_sale_quotation
    ADD COLUMN IF NOT EXISTS gold_markup NUMERIC;
