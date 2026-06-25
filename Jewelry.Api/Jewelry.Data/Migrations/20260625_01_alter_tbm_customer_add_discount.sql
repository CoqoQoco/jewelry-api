-- Migration: Add discount to Customer (0-99)
-- Date: 2026-06-25
ALTER TABLE tbm_customer
    ADD COLUMN IF NOT EXISTS discount INTEGER NOT NULL DEFAULT 0;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'tbm_customer_discount_ck') THEN
        ALTER TABLE tbm_customer
            ADD CONSTRAINT tbm_customer_discount_ck CHECK (discount >= 0 AND discount <= 99);
    END IF;
END $$;
