-- =============================================
-- Migration: Create tbm_casting_material
-- Date: 2026-08-17
-- Description: master keyword สำหรับคัดกรอง "วัตถุดิบงานแต่ง" (ProductionPlanStatus.Casting)
--              เมื่อเบิกวัตถุดิบพลอย/วัสดุลง Job — ใช้แทน hardcode array ใน
--              ReceiptAndIssueStockGemService.IsCastingMaterial
-- =============================================

-- =============================================
-- 1. tbm_casting_material
-- =============================================
CREATE TABLE IF NOT EXISTS tbm_casting_material (
    id              SERIAL,
    code            CHARACTER VARYING NOT NULL,
    -- keyword ที่ใช้จับคู่กับ group_name หรือ shape ของวัตถุดิบในคลัง (tbt_stock_gem)
    name_th         CHARACTER VARYING NOT NULL,
    name_en         CHARACTER VARYING NOT NULL,
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    create_date     TIMESTAMPTZ NOT NULL,
    create_by       CHARACTER VARYING NOT NULL,
    update_date     TIMESTAMPTZ,
    update_by       CHARACTER VARYING,
    CONSTRAINT tbm_casting_material_pk PRIMARY KEY (id),
    CONSTRAINT tbm_casting_material_code_uq UNIQUE (code)
);

-- =============================================
-- 2. Seed keyword (ย้ายจาก hardcode array เดิม + คำที่ตกหล่น)
-- =============================================
INSERT INTO tbm_casting_material (code, name_th, name_en, is_active, create_date, create_by) VALUES
    ('CHAIN', 'สร้อย (shape CHAIN)', 'chain', TRUE, NOW(), 'SYSTEM'),
    ('สร้อย', 'สร้อย', 'chain', TRUE, NOW(), 'SYSTEM'),
    ('สปริง', 'สปริง', 'spring', TRUE, NOW(), 'SYSTEM'),
    ('ก้ามปู', 'ก้ามปู', 'lobster clasp', TRUE, NOW(), 'SYSTEM'),
    ('กำไล', 'กำไล', 'bangle', TRUE, NOW(), 'SYSTEM'),
    ('ลูกบอล', 'ลูกบอล', 'ball', TRUE, NOW(), 'SYSTEM'),
    ('ลุกบอล', 'ลูกบอล (สะกดผิดในข้อมูลจริง)', 'ball (legacy misspelling)', TRUE, NOW(), 'SYSTEM'),
    ('แป้น', 'แป้น (ตัวเรือนต่างหู)', 'earring back/plate', TRUE, NOW(), 'SYSTEM'),
    ('ก้ามกุ้ง', 'ก้ามกุ้ง', 'hook clasp', TRUE, NOW(), 'SYSTEM'),
    ('ตะขอ', 'ตะขอ', 'hook', TRUE, NOW(), 'SYSTEM'),
    ('เนื้อเงิน', 'เนื้อเงิน', 'silver', TRUE, NOW(), 'SYSTEM')
ON CONFLICT (code) DO NOTHING;
