-- =============================================
-- Migration: Customer Product Gallery
-- Date: 2026-09-22
-- Description: ตารางเก็บรูปแกลเลอรีสินค้าสำหรับลูกค้า (หลายรูปต่อสินค้า)
--   แยกจากรูป internal เดิมของ tbt_sku (image_path/image_name) และ tbt_stock_product_image โดยสิ้นเชิง
--   ไม่มีการ backfill — ตารางเริ่มต้นว่างเปล่า
--   scope_type = MOLD  -> ใช้ร่วมกันทุกชิ้นที่ mold เดียวกัน, scope_key = UPPER(TRIM(mold))
--   scope_type = SKU   -> เฉพาะชิ้นนั้น (sku_code), scope_key = sku_code ตามที่เก็บจริง
-- Re-run safety: Idempotent — CREATE TABLE/INDEX IF NOT EXISTS, รันซ้ำได้ไม่ error
-- =============================================

CREATE TABLE IF NOT EXISTS tbt_product_gallery (
    id              BIGSERIAL NOT NULL,
    scope_type      CHARACTER VARYING(10) NOT NULL,
    scope_key       CHARACTER VARYING(100) NOT NULL,
    blob_path       CHARACTER VARYING(500) NOT NULL,
    sort_order      INT NOT NULL DEFAULT 0,
    width           INT,
    height          INT,
    size_bytes      BIGINT,
    content_type    CHARACTER VARYING(50),
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    create_date     TIMESTAMPTZ NOT NULL,
    create_by       CHARACTER VARYING NOT NULL,
    update_date     TIMESTAMPTZ,
    update_by       CHARACTER VARYING,
    CONSTRAINT tbt_product_gallery_pk PRIMARY KEY (id),
    CONSTRAINT tbt_product_gallery_scope_type_ck CHECK (scope_type IN ('MOLD', 'SKU'))
);

CREATE INDEX IF NOT EXISTS idx_tbt_product_gallery_scope_active
    ON tbt_product_gallery (scope_type, scope_key, sort_order)
    WHERE is_active;
