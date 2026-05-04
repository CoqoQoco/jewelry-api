-- =============================================
-- Migration: Bank Master Table + Payment Bank Columns
-- Date: 2026-05-04
-- Description: สร้าง master table ธนาคาร และเพิ่ม columns bank_code, bank_branch ใน tbt_sale_invoice_payment_item
-- =============================================

-- =============================================
-- 1. tbm_bank
-- =============================================
CREATE TABLE IF NOT EXISTS tbm_bank (
    code            CHARACTER VARYING NOT NULL,
    name_th         CHARACTER VARYING,
    name_en         CHARACTER VARYING,
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    create_by       CHARACTER VARYING NOT NULL,
    create_date     TIMESTAMPTZ NOT NULL,
    update_by       CHARACTER VARYING,
    update_date     TIMESTAMPTZ,
    CONSTRAINT tbm_bank_pk PRIMARY KEY (code)
);

-- =============================================
-- 2. Seed ธนาคารหลักของไทย
-- =============================================
INSERT INTO tbm_bank (code, name_th, name_en, is_active, create_by, create_date)
VALUES
    ('KBANK', 'กสิกรไทย',          'Kasikorn Bank',                   TRUE, 'SYSTEM', NOW())
ON CONFLICT DO NOTHING;

INSERT INTO tbm_bank (code, name_th, name_en, is_active, create_by, create_date)
VALUES
    ('SCB',   'ไทยพาณิชย์',        'Siam Commercial Bank',            TRUE, 'SYSTEM', NOW())
ON CONFLICT DO NOTHING;

INSERT INTO tbm_bank (code, name_th, name_en, is_active, create_by, create_date)
VALUES
    ('BBL',   'กรุงเทพ',           'Bangkok Bank',                    TRUE, 'SYSTEM', NOW())
ON CONFLICT DO NOTHING;

INSERT INTO tbm_bank (code, name_th, name_en, is_active, create_by, create_date)
VALUES
    ('KTB',   'กรุงไทย',           'Krungthai Bank',                  TRUE, 'SYSTEM', NOW())
ON CONFLICT DO NOTHING;

INSERT INTO tbm_bank (code, name_th, name_en, is_active, create_by, create_date)
VALUES
    ('BAY',   'กรุงศรีอยุธยา',     'Bank of Ayudhya',                 TRUE, 'SYSTEM', NOW())
ON CONFLICT DO NOTHING;

INSERT INTO tbm_bank (code, name_th, name_en, is_active, create_by, create_date)
VALUES
    ('TTB',   'ทหารไทยธนชาต',      'TMBThanachart Bank',              TRUE, 'SYSTEM', NOW())
ON CONFLICT DO NOTHING;

INSERT INTO tbm_bank (code, name_th, name_en, is_active, create_by, create_date)
VALUES
    ('GSB',   'ออมสิน',            'Government Savings Bank',         TRUE, 'SYSTEM', NOW())
ON CONFLICT DO NOTHING;

INSERT INTO tbm_bank (code, name_th, name_en, is_active, create_by, create_date)
VALUES
    ('TISCO', 'ทิสโก้',            'TISCO Bank',                      TRUE, 'SYSTEM', NOW())
ON CONFLICT DO NOTHING;

INSERT INTO tbm_bank (code, name_th, name_en, is_active, create_by, create_date)
VALUES
    ('KKP',   'เกียรตินาคินภัทร',  'Kiatnakin Phatra Bank',           TRUE, 'SYSTEM', NOW())
ON CONFLICT DO NOTHING;

INSERT INTO tbm_bank (code, name_th, name_en, is_active, create_by, create_date)
VALUES
    ('CIMB',  'ซีไอเอ็มบีไทย',    'CIMB Thai Bank',                  TRUE, 'SYSTEM', NOW())
ON CONFLICT DO NOTHING;

INSERT INTO tbm_bank (code, name_th, name_en, is_active, create_by, create_date)
VALUES
    ('UOB',   'ยูโอบี',            'United Overseas Bank (Thai)',     TRUE, 'SYSTEM', NOW())
ON CONFLICT DO NOTHING;

INSERT INTO tbm_bank (code, name_th, name_en, is_active, create_by, create_date)
VALUES
    ('LH',    'แลนด์ แอนด์ เฮ้าส์', 'Land and Houses Bank',          TRUE, 'SYSTEM', NOW())
ON CONFLICT DO NOTHING;

INSERT INTO tbm_bank (code, name_th, name_en, is_active, create_by, create_date)
VALUES
    ('ICBC',  'ไอซีบีซี ไทย',      'ICBC (Thai)',                     TRUE, 'SYSTEM', NOW())
ON CONFLICT DO NOTHING;

-- =============================================
-- 3. เพิ่ม columns ใน tbt_sale_invoice_payment_item
-- =============================================
ALTER TABLE tbt_sale_invoice_payment_item
    ADD COLUMN IF NOT EXISTS bank_code   CHARACTER VARYING,
    ADD COLUMN IF NOT EXISTS bank_branch CHARACTER VARYING;
