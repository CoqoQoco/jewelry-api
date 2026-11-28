-- =============================================
-- Migration: คืนพลอยจากใบงานเข้าคลัง (ประวัติ + permission)
-- Date: 2026-11-28
-- Ticket: TK202611280001
-- Description: สร้าง tbt_production_plan_gem_return เก็บประวัติการคืนพลอยจากแถวพลอยของใบงาน
--   mode: RETURN_NOW = สร้างรายการรับเข้า INB ใหม่พร้อมบวกสต็อก, LINK_EXISTING = ผูกกับรายการ INB ที่รับเข้าไปแล้ว (ไม่แตะสต็อก)
--   reason: DUPLICATE | WRONG_PLAN | OVER | DAMAGED | OTHER
--   seed permission stock-gem:return-from-plan ให้ Dev, Admin เท่านั้น
--   role คลังพลอย (gem-stock) ให้ user กำหนดเองที่หน้าจัดการสิทธิ์ · user ต้อง logout + login ใหม่หลังรัน
-- ลำดับ deploy: รันก่อน deploy API · รันซ้ำได้ (idempotent)
-- Rollback: 20261128_04_plan_gem_return_rollback.sql
-- =============================================

CREATE TABLE IF NOT EXISTS tbt_production_plan_gem_return (
    id                      SERIAL NOT NULL,
    production_plan_id      INT NOT NULL,
    header_id               INT,
    item_no                 CHARACTER VARYING NOT NULL,
    outbound_running        CHARACTER VARYING,
    gem_code                CHARACTER VARYING NOT NULL,
    return_qty              NUMERIC NOT NULL,
    return_weight           NUMERIC NOT NULL,
    mode                    CHARACTER VARYING(20) NOT NULL,
    -- mode: RETURN_NOW | LINK_EXISTING
    inbound_transection_id  BIGINT,
    inbound_running         CHARACTER VARYING,
    reason                  CHARACTER VARYING(30) NOT NULL,
    -- reason: DUPLICATE | WRONG_PLAN | OVER | DAMAGED | OTHER
    remark                  CHARACTER VARYING,
    create_by               CHARACTER VARYING,
    create_date             TIMESTAMPTZ NOT NULL DEFAULT now(),
    CONSTRAINT tbt_production_plan_gem_return_pk PRIMARY KEY (id)
);

ALTER TABLE tbt_production_plan_gem_return ADD COLUMN IF NOT EXISTS header_id INT;

CREATE INDEX IF NOT EXISTS idx_tbt_production_plan_gem_return_plan
    ON tbt_production_plan_gem_return (production_plan_id);
CREATE INDEX IF NOT EXISTS idx_tbt_production_plan_gem_return_inbound
    ON tbt_production_plan_gem_return (inbound_transection_id);

INSERT INTO tbm_permission (code, name, group_name, create_by) VALUES
    ('stock-gem:return-from-plan', 'คืนพลอยจากใบงานเข้าคลัง', 'Stock Gem', 'system')
ON CONFLICT (code) DO NOTHING;

INSERT INTO tbt_role_permission (role_id, permission_id, create_by)
SELECT r.id, p.id, 'system'
FROM tbm_user_role r
CROSS JOIN tbm_permission p
WHERE r.name IN ('Dev', 'Admin') AND r.is_active = TRUE
    AND p.code = 'stock-gem:return-from-plan' AND p.is_active = TRUE
ON CONFLICT (role_id, permission_id) DO NOTHING;
