-- =============================================
-- Migration: Material stock policy + per-item minimum (MaterialInsight - คลังวัตถุดิบ)
-- Date: 2026-10-22
-- Description: ตารางตั้งค่าของแท็บ "คลังวัตถุดิบ" (/executive) — ใช้ทั้งทอง (GOLD) และพลอย (GEM ในอนาคต)
--   1. tbm_material_stock_policy : นโยบายต่อประเภทวัตถุดิบ 1 แถว (material_kind = 'GOLD' | 'GEM')
--      ใช้คำนวณ "ขั้นต่ำอัตโนมัติ" = CEIL(ใช้เฉลี่ย/วัน x (lead_time_days + safety_days))
--      และเกณฑ์ใกล้ขั้นต่ำ / ของตาย / ของค้างในกระบวนการ
--   2. tbm_material_stock_min : ขั้นต่ำ override ราย item (gold: item_key = '{gold_code}|{gold_size_code}')
--      is_active = false -> ยกเลิก override (เก็บประวัติ, save ซ้ำจะเปิดใช้งานอีกครั้ง)
--   ต้อง run migration นี้ "ก่อน" deploy API (ลำดับ SQL -> API -> UI)
-- Run order: 01 (ไฟล์นี้) -> 02 seed policy -> 03 seed permission
-- Re-run safety: Idempotent — CREATE TABLE IF NOT EXISTS
-- =============================================

-- =============================================
-- 1. tbm_material_stock_policy
-- =============================================
CREATE TABLE IF NOT EXISTS tbm_material_stock_policy (
    material_kind           CHARACTER VARYING NOT NULL,
    -- 'GOLD' | 'GEM'
    window_months           INT NOT NULL,
    -- หน้าต่างเฉลี่ยอัตราใช้ (เดือนย้อนหลังจากวันนี้)
    lead_time_days          INT NOT NULL,
    -- ระยะเวลาสั่งของมาถึง (วัน)
    safety_days             INT NOT NULL,
    -- สำรองเพิ่ม (วัน)
    floor_qty               NUMERIC NOT NULL,
    -- ขั้นต่ำพื้น (หน่วยของวัตถุดิบ: ทอง = กรัม) effective min = MAX(auto, floor) ถ้าไม่มี override
    near_percent            NUMERIC NOT NULL,
    -- ใกล้ขั้นต่ำเมื่อคงเหลือ < ขั้นต่ำ x near_percent / 100 (125 = เกินขั้นต่ำไม่ถึง 25%)
    dead_stock_days         INT NOT NULL,
    -- ไม่มีการใช้ (เบิก) เกินกี่วันทั้งที่มีของ = ของตาย
    forecast_months         INT NOT NULL,
    on_process_stale_days    INT NOT NULL,
    -- ของค้างในกระบวนการ/ที่ช่างเกินกี่วัน
    price_change_percent    NUMERIC NOT NULL,
    -- (พลอย) ราคาเปลี่ยน % เตือน
    concentration_percent   NUMERIC NOT NULL,
    -- (พลอย) สัดส่วนรวมศูนย์ % เตือน
    min_history_months      INT NOT NULL,
    -- ต้องมีประวัติการใช้จริงอย่างน้อยกี่เดือนเต็มถึงจะออก forecast
    create_by               CHARACTER VARYING NOT NULL,
    create_date             TIMESTAMPTZ NOT NULL,
    update_by               CHARACTER VARYING,
    update_date             TIMESTAMPTZ,
    CONSTRAINT tbm_material_stock_policy_pk PRIMARY KEY (material_kind)
);

-- =============================================
-- 2. tbm_material_stock_min
-- =============================================
CREATE TABLE IF NOT EXISTS tbm_material_stock_min (
    id                      BIGSERIAL NOT NULL,
    material_kind           CHARACTER VARYING NOT NULL,
    item_key                CHARACTER VARYING NOT NULL,
    -- gold: '{gold_code}|{gold_size_code}'
    min_qty                 NUMERIC NOT NULL,
    -- ขั้นต่ำ override (0 = ตั้งใจให้ไม่มีขั้นต่ำ)
    lead_time_days          INT,
    -- override lead time ราย item (null = ใช้ของ policy)
    note                    CHARACTER VARYING(500),
    is_active               BOOLEAN NOT NULL DEFAULT TRUE,
    create_by               CHARACTER VARYING NOT NULL,
    create_date             TIMESTAMPTZ NOT NULL,
    update_by               CHARACTER VARYING,
    update_date             TIMESTAMPTZ,
    CONSTRAINT tbm_material_stock_min_pk PRIMARY KEY (id),
    CONSTRAINT tbm_material_stock_min_kind_item_uq UNIQUE (material_kind, item_key)
);
-- หมายเหตุ: UNIQUE (material_kind, item_key) สร้าง index ให้อัตโนมัติแล้ว ไม่ต้องเพิ่ม index ซ้ำ
