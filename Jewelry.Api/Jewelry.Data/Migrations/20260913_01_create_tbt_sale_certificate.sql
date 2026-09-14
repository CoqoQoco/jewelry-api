-- =============================================
-- Migration: Create tbt_sale_certificate
-- Date: 2026-09-13
-- Description: บันทึกสแนปช็อตใบรับรองสินค้า (Certificate) แบบเต็ม — 1 แถวต่อ 1 ใบที่ออก
--              รองรับ branding ลูกค้า (โลโก้/ชื่อแบรนด์) + รูปใหม่ต่อใบ + ประวัติเต็มรูปแบบ
--              (เดิมเก็บผ่าน tbt_sale_invoice_print_log paper_type='certificate' แต่เก็บ
--              เฉพาะ certificateNo/stockNumber ทำให้ค่า field ต่อแผ่นหายไปหมด)
-- Run order: ไม่มี dependency กับตารางอื่น รันได้ทันที (backfill อ่านจาก tbt_sale_invoice_print_log)
-- Re-run safety: Idempotent — CREATE TABLE/INDEX IF NOT EXISTS + backfill ใช้ WHERE NOT EXISTS
-- =============================================

-- =============================================
-- 1. tbt_sale_certificate
-- =============================================
CREATE TABLE IF NOT EXISTS tbt_sale_certificate (
    running          CHARACTER VARYING NOT NULL,
    batch            CHARACTER VARYING NOT NULL,
    certificate_no   CHARACTER VARYING NOT NULL,
    issue_no         INT NOT NULL,
    invoice_running  CHARACTER VARYING,
    invoice_no       CHARACTER VARYING NOT NULL,
    stock_number     CHARACTER VARYING NOT NULL,
    item_no          CHARACTER VARYING,
    customer_code    CHARACTER VARYING,
    brand_mode       CHARACTER VARYING(20) NOT NULL DEFAULT 'dk',
    brand_name       CHARACTER VARYING,
    brand_logo_path  CHARACTER VARYING,
    image_path       CHARACTER VARYING,
    signer_title     CHARACTER VARYING,
    data             JSONB NOT NULL,
    create_by        CHARACTER VARYING NOT NULL,
    create_date      TIMESTAMPTZ NOT NULL,
    CONSTRAINT tbt_sale_certificate_pk PRIMARY KEY (running)
);

CREATE INDEX IF NOT EXISTS idx_tbt_sale_certificate_invoice_no ON tbt_sale_certificate(invoice_no);
CREATE INDEX IF NOT EXISTS idx_tbt_sale_certificate_certificate_no ON tbt_sale_certificate(certificate_no);
CREATE INDEX IF NOT EXISTS idx_tbt_sale_certificate_stock_number ON tbt_sale_certificate(stock_number);
CREATE INDEX IF NOT EXISTS idx_tbt_sale_certificate_customer_code ON tbt_sale_certificate(customer_code);

COMMENT ON COLUMN tbt_sale_certificate.batch IS 'จัดกลุ่มใบที่ออกพร้อมกันจากการเรียก Create 1 ครั้ง (1 รอบออกใบ)';
COMMENT ON COLUMN tbt_sale_certificate.issue_no IS 'ลำดับการออกใบซ้ำของ certificate_no เดียวกัน (1,2,3...) — silver lot qty N ออกได้ N แผ่นเลขเดียวกันในรอบเดียว';
COMMENT ON COLUMN tbt_sale_certificate.brand_mode IS 'dk = โลโก้/แบรนด์ดวงแก้ว, customer = โลโก้/แบรนด์ลูกค้า';
COMMENT ON COLUMN tbt_sale_certificate.brand_logo_path IS 'blob name เช่น Certificate/logo-xxxx.png';
COMMENT ON COLUMN tbt_sale_certificate.image_path IS 'blob name ของรูปใหม่ที่อัปสำหรับใบนี้ — NULL = ใช้รูป stock เดิม';
COMMENT ON COLUMN tbt_sale_certificate.data IS 'สแนปช็อตข้อมูลเต็มของแผ่นนี้ตามที่ UI ส่งมา';

-- =============================================
-- 2. Backfill legacy certificate print logs (tbt_sale_invoice_print_log, paper_type='certificate')
--    ตัวอย่างข้อมูลเดิม: {"count":1,"signerTitle":"General Manager","certificates":[{"stockNumber":"...","certificateNo":"..."}],"stockNumbers":[...]}
--    running เดิมใช้ prefix LEGACY- กันชนกับ convention ใหม่ (CERT...)
-- =============================================
INSERT INTO tbt_sale_certificate (
    running, batch, certificate_no, issue_no, invoice_running, invoice_no,
    stock_number, item_no, customer_code, brand_mode, brand_name, brand_logo_path,
    image_path, signer_title, data, create_by, create_date
)
SELECT
    'LEGACY-' || pl.running || '-' || cert.idx AS running,
    pl.running AS batch,
    cert.value ->> 'certificateNo' AS certificate_no,
    ROW_NUMBER() OVER (PARTITION BY cert.value ->> 'certificateNo' ORDER BY pl.printed_at, cert.idx) AS issue_no,
    pl.invoice_running,
    pl.invoice_no,
    cert.value ->> 'stockNumber' AS stock_number,
    NULL AS item_no,
    NULL AS customer_code,
    'dk' AS brand_mode,
    NULL AS brand_name,
    NULL AS brand_logo_path,
    NULL AS image_path,
    pl.data ->> 'signerTitle' AS signer_title,
    jsonb_build_object(
        'legacy', true,
        'certificateNo', cert.value ->> 'certificateNo',
        'stockNumber', cert.value ->> 'stockNumber',
        'signerTitle', pl.data ->> 'signerTitle'
    ) AS data,
    pl.printed_by AS create_by,
    pl.printed_at AS create_date
FROM tbt_sale_invoice_print_log pl
CROSS JOIN LATERAL jsonb_array_elements(COALESCE(pl.data -> 'certificates', '[]'::jsonb)) WITH ORDINALITY AS cert(value, idx)
WHERE pl.paper_type = 'certificate'
  AND NOT EXISTS (
      SELECT 1 FROM tbt_sale_certificate c WHERE c.batch = pl.running
  );

-- =============================================
-- Rollback:
-- DROP INDEX IF EXISTS idx_tbt_sale_certificate_customer_code;
-- DROP INDEX IF EXISTS idx_tbt_sale_certificate_stock_number;
-- DROP INDEX IF EXISTS idx_tbt_sale_certificate_certificate_no;
-- DROP INDEX IF EXISTS idx_tbt_sale_certificate_invoice_no;
-- DROP TABLE IF EXISTS tbt_sale_certificate;
-- =============================================
