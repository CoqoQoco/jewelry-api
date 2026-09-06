-- =============================================
-- Migration: Create silver transfer tables
-- Date: 2026-09-06
-- Description: รองรับโอนสต็อกงานเงินจากระบบเก่า (batch 5-9-2026)
--              stock_silver      = คิวโอน (mirror stock_9k / stock_14k) — qty = จำนวนชิ้นที่ต้องสร้างจากแถวนั้น
--              stock_import_sil  = staging สำหรับ \copy จาก mdb งานเงิน
-- Run order: ก่อนโหลด master/queue ของ batch 2026-09-06
--
-- หมายเหตุสำคัญ: mdb งานเงินมี 68 คอลัมน์ ไม่ใช่ 70 เหมือน 9K/18K
--                (ไม่มี typedesc และ dateadd) และลำดับคอลัมน์ต่างจาก stock_import_9k
--                ลำดับด้านล่างตรงกับไฟล์ master_sil.csv แบบ 1:1 ห้ามสลับ
-- ชนิดคอลัมน์อิงตาม stock (ตารางปลายทาง) เพื่อให้ INSERT ... SELECT ทำงานได้โดยไม่ต้อง cast
-- =============================================

-- =============================================
-- 1. stock_silver — คิวโอน
-- =============================================
CREATE TABLE IF NOT EXISTS stock_silver (
    id          SERIAL,
    no_product  CHARACTER VARYING,
    style_no    CHARACTER VARYING,
    qty         INTEGER,
    -- qty = จำนวนชิ้นที่ต้องสร้างจากแถวนี้ (งานเงิน 1 รหัสมีได้หลายชิ้น สูงสุด 80)
    -- ยึดตัวเลขจาก Excel ของลูกค้า ไม่ใช่ stock.quantity ของระบบเก่า (ต่างกัน 1,867 จาก 2,345 รหัส)
    is_transfer BOOLEAN NOT NULL DEFAULT FALSE,
    CONSTRAINT stock_silver_pk PRIMARY KEY (id)
);

CREATE INDEX IF NOT EXISTS idx_stock_silver_no_product ON stock_silver(no_product);
CREATE INDEX IF NOT EXISTS idx_stock_silver_is_transfer ON stock_silver(is_transfer);

-- =============================================
-- 2. stock_import_sil — staging (68 คอลัมน์ ลำดับตรงกับ master_sil.csv)
-- =============================================
CREATE TABLE IF NOT EXISTS stock_import_sil (
    recgen      NUMERIC,
    stockid     NUMERIC,
    typejob     CHARACTER VARYING,
    username    CHARACTER VARYING,
    noproduct   CHARACTER VARYING,
    partnoold   CHARACTER VARYING,
    whno        CHARACTER VARYING,
    partno      CHARACTER VARYING,
    issueno     CHARACTER VARYING,
    typep       CHARACTER VARYING,
    jobno       CHARACTER VARYING,
    codeproduct CHARACTER VARYING,
    no_code     CHARACTER VARYING,
    productname CHARACTER VARYING,
    descthai    CHARACTER VARYING,
    dateproduct CHARACTER VARYING,
    status_p    CHARACTER VARYING,
    quantity    NUMERIC,
    unit        CHARACTER VARYING,
    pricecost   CHARACTER VARYING,
    pricesale   NUMERIC,
    priceus     NUMERIC,
    remark      CHARACTER VARYING,
    ringsize    CHARACTER VARYING,
    typeg       CHARACTER VARYING,
    qtyg        NUMERIC,
    wg          NUMERIC,
    unit1       CHARACTER VARYING,
    priceg      NUMERIC,
    typesil     CHARACTER VARYING,
    wsil        NUMERIC,
    unitsil     CHARACTER VARYING,
    typed       CHARACTER VARYING,
    qtyd        NUMERIC,
    wd          NUMERIC,
    unit2       CHARACTER VARYING,
    priced      NUMERIC,
    origin_d    CHARACTER VARYING,
    typer       CHARACTER VARYING,
    qtyr        NUMERIC,
    wr          NUMERIC,
    sizer       CHARACTER VARYING,
    unit3       CHARACTER VARYING,
    pricer      NUMERIC,
    "TypeS"     CHARACTER VARYING,
    qtys        NUMERIC,
    ws          NUMERIC,
    sizes       CHARACTER VARYING,
    unit4       CHARACTER VARYING,
    prices      NUMERIC,
    typee       CHARACTER VARYING,
    qtye        NUMERIC,
    we          NUMERIC,
    sizee       CHARACTER VARYING,
    unit5       CHARACTER VARYING,
    pricee      NUMERIC,
    typem       CHARACTER VARYING,
    qtym        NUMERIC,
    wm          NUMERIC,
    sizem       CHARACTER VARYING,
    unit6       CHARACTER VARYING,
    pricem      NUMERIC,
    typed1      CHARACTER VARYING,
    qtyd1       NUMERIC,
    wd1         NUMERIC,
    unitd1      CHARACTER VARYING,
    priced1     NUMERIC,
    origin_d1   CHARACTER VARYING
);
