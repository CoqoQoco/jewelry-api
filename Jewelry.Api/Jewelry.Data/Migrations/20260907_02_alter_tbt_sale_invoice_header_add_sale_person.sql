-- =============================================
-- Migration: Add sale_person, sale_support to tbt_sale_invoice_header
-- Date: 2026-09-07
-- Description: สแนปช็อตผู้ขาย (SALE) และผู้ช่วยขาย (SUPPORT) จากใบสั่งขายมาเก็บไว้ที่ใบแจ้งหนี้ ณ ตอนสร้าง เพื่อไม่ให้ใบพิมพ์เปลี่ยนตามใบสั่งขายที่แก้ไขภายหลัง
-- Run order: รันหลัง 20260907_01_alter_tbt_sale_order_add_sale_person.sql
-- =============================================

ALTER TABLE tbt_sale_invoice_header
    ADD COLUMN IF NOT EXISTS sale_person CHARACTER VARYING,
    ADD COLUMN IF NOT EXISTS sale_support CHARACTER VARYING;
