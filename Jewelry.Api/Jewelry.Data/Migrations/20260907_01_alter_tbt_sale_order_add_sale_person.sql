-- =============================================
-- Migration: Add sale_person, sale_support to tbt_sale_order
-- Date: 2026-09-07
-- Description: บันทึกผู้ขาย (SALE) และผู้ช่วยขาย (SUPPORT) บนใบสั่งขาย — เก็บเป็นชื่อ (free text) ไม่ผูก FK
-- =============================================

ALTER TABLE tbt_sale_order
    ADD COLUMN IF NOT EXISTS sale_person CHARACTER VARYING,
    ADD COLUMN IF NOT EXISTS sale_support CHARACTER VARYING;
