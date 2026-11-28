-- Rollback: 20261128_01_alter_sale_invoice_header_add_vat_included.sql
-- ลำดับ: รัน 20261128_02_..._rollback.sql ก่อนเสมอ แล้วค่อยไฟล์นี้
-- ต้อง rollback 20261128_02 (กู้จาก bak_20261128_invoice_money) และถอย API ก่อน
ALTER TABLE tbt_sale_invoice_header
    DROP COLUMN IF EXISTS vat_included;
