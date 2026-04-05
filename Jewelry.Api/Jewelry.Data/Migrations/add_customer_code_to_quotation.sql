-- Add customer_code column to tbt_sale_quotation for reference back to tbm_customer
ALTER TABLE tbt_sale_quotation ADD COLUMN IF NOT EXISTS customer_code VARCHAR(50);
