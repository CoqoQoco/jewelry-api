-- =============================================
-- Migration: Rename Pre-Plan tables PascalCase → snake_case
-- Date: 2026-05-10
-- Description:
--   Project convention ใช้ snake_case แต่ migration เก่า refactor_pre_plan_to_item_material.sql
--   สร้างเป็น PascalCase ผิด convention → drop และ recreate
--   DB ไม่มีข้อมูลในตารางนี้ → ปลอดภัย
--
-- ⚠️  วิธีรัน: ต้อง Execute เป็น "SCRIPT" ทั้งไฟล์ ไม่ใช่ statement เดียว
--    DBeaver: Alt+X (Execute SQL Script) — ห้ามใช้ Ctrl+Enter (Execute Statement)
--    psql: psql -d jewelry -f rename_pre_plan_tables_to_snake_case.sql
-- =============================================

BEGIN;

-- Drop เก่าทั้ง PascalCase และ snake_case (กัน partial state)
DROP TABLE IF EXISTS "TbtProductionPrePlanMaterial" CASCADE;
DROP TABLE IF EXISTS "TbtProductionPrePlanItem" CASCADE;
DROP TABLE IF EXISTS "TbtProductionPrePlan" CASCADE;
DROP TABLE IF EXISTS tbt_production_pre_plan_material CASCADE;
DROP TABLE IF EXISTS tbt_production_pre_plan_item CASCADE;
DROP TABLE IF EXISTS tbt_production_pre_plan CASCADE;

-- =============================================
-- 1. tbt_production_pre_plan (Header)
-- =============================================
CREATE TABLE tbt_production_pre_plan (
    id                  SERIAL,
    order_no            CHARACTER VARYING,
    production_round    INT,
    job_type            CHARACTER VARYING,
    job_location        CHARACTER VARYING,
    gold_type           CHARACTER VARYING,
    order_date          TIMESTAMPTZ NOT NULL,
    delivery_date       TIMESTAMPTZ NOT NULL,
    remark              TEXT,
    status              CHARACTER VARYING NOT NULL DEFAULT 'Draft',
    -- Status: Draft, Submitted, Approved, Rejected, Consumed
    reject_reason       TEXT,
    create_by           CHARACTER VARYING,
    create_date         TIMESTAMPTZ,
    update_by           CHARACTER VARYING,
    update_date         TIMESTAMPTZ,
    submit_by           CHARACTER VARYING,
    submit_date         TIMESTAMPTZ,
    approve_by          CHARACTER VARYING,
    approve_date        TIMESTAMPTZ,
    CONSTRAINT tbt_production_pre_plan_pk PRIMARY KEY (id)
);

-- =============================================
-- 2. tbt_production_pre_plan_item
-- =============================================
CREATE TABLE tbt_production_pre_plan_item (
    id                          SERIAL,
    pre_plan_id                 INT NOT NULL,
    item_no                     INT NOT NULL,
    mold_code                   CHARACTER VARYING NOT NULL,
    mold_detail                 TEXT,
    product_type                CHARACTER VARYING,
    product_qty                 INT,
    product_qty_unit            CHARACTER VARYING,
    product_detail              TEXT,
    product_image_path          CHARACTER VARYING,
    linked_production_plan_id   INT NULL,
    create_by                   CHARACTER VARYING,
    create_date                 TIMESTAMPTZ,
    update_by                   CHARACTER VARYING,
    update_date                 TIMESTAMPTZ,
    CONSTRAINT tbt_production_pre_plan_item_pk PRIMARY KEY (id),
    CONSTRAINT tbt_production_pre_plan_item_pre_plan_fk
        FOREIGN KEY (pre_plan_id)
        REFERENCES tbt_production_pre_plan(id)
        ON DELETE CASCADE
);

CREATE INDEX idx_tbt_production_pre_plan_item_pre_plan_id
    ON tbt_production_pre_plan_item(pre_plan_id);

-- =============================================
-- 3. tbt_production_pre_plan_material
-- =============================================
CREATE TABLE tbt_production_pre_plan_material (
    id                   SERIAL,
    pre_plan_item_id     INT NOT NULL,
    gold                 CHARACTER VARYING,
    gold_size            CHARACTER VARYING,
    gold_qty             NUMERIC(18,3),
    gem                  CHARACTER VARYING,
    gem_shape            CHARACTER VARYING,
    gem_qty              NUMERIC(18,3),
    gem_unit             CHARACTER VARYING,
    gem_size             CHARACTER VARYING,
    gem_weight           NUMERIC(18,3),
    gem_weight_unit      CHARACTER VARYING,
    diamond_qty          NUMERIC(18,3),
    diamond_unit         CHARACTER VARYING,
    diamond_size         CHARACTER VARYING,
    diamond_weight       NUMERIC(18,3),
    diamond_weight_unit  CHARACTER VARYING,
    diamond_quality      CHARACTER VARYING,
    create_by            CHARACTER VARYING,
    create_date          TIMESTAMPTZ,
    CONSTRAINT tbt_production_pre_plan_material_pk PRIMARY KEY (id),
    CONSTRAINT tbt_production_pre_plan_material_item_fk
        FOREIGN KEY (pre_plan_item_id)
        REFERENCES tbt_production_pre_plan_item(id)
        ON DELETE CASCADE
);

CREATE INDEX idx_tbt_production_pre_plan_material_item_id
    ON tbt_production_pre_plan_material(pre_plan_item_id);

COMMIT;
