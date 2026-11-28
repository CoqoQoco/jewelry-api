-- Migration: Add vat_included to tbt_sale_invoice_header (TK202611280005)
-- Date: 2026-11-28
-- true = invoice THB ที่ราคารวม VAT 7% แล้ว (vat ของ header คงเป็น 0) ผู้อ่านทุกจุดต้องดู vat_included
-- รันก่อน deploy API
ALTER TABLE tbt_sale_invoice_header
    ADD COLUMN IF NOT EXISTS vat_included BOOLEAN NOT NULL DEFAULT false;
