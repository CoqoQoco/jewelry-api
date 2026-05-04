-- =============================================
-- Migration: Production Pre-Plan Material
-- Date: 2026-05-03
-- Description: สร้าง table รายการวัสดุของใบสั่งผลิต (Pre-Plan Material)
-- =============================================

-- =============================================
-- 1. tbt_production_pre_plan_material
-- =============================================
CREATE TABLE IF NOT EXISTS tbt_production_pre_plan_material (
    id              SERIAL,
    pre_plan_id     INTEGER NOT NULL,
    material_type   CHARACTER VARYING(20) NOT NULL,
    -- material_type: 'Gem' / 'Diamond' / 'Gold'
    master_id       INTEGER,
    material_code   CHARACTER VARYING(100),
    shape_code      CHARACTER VARYING(50),
    size            CHARACTER VARYING(50),
    qty             INTEGER NOT NULL DEFAULT 0,
    color           CHARACTER VARYING(50),
    weight          DECIMAL(10, 4),
    weight_unit     CHARACTER VARYING(20),
    is_locked       BOOLEAN NOT NULL DEFAULT FALSE,
    remark          CHARACTER VARYING(200),
    create_by       CHARACTER VARYING(100) NOT NULL,
    create_date     TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    update_by       CHARACTER VARYING(100),
    update_date     TIMESTAMPTZ,
    CONSTRAINT tbt_production_pre_plan_material_pk PRIMARY KEY (id),
    CONSTRAINT tbt_production_pre_plan_material_pre_plan_fk FOREIGN KEY (pre_plan_id)
        REFERENCES tbt_production_pre_plan(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_tbt_production_pre_plan_material_pre_plan_id ON tbt_production_pre_plan_material(pre_plan_id);
