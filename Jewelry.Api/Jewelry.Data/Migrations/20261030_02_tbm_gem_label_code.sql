-- =============================================
-- Migration: tbm_gem label abbreviation + label sort order (TK202610290005)
-- Date: 2026-10-30
-- Description: label_code = ตัวย่อที่พิมพ์บนป้าย (null = ใช้ code)
--              label_sort = ลำดับบนป้าย (null = ต่อท้ายตัวที่มีลำดับ แล้วเรียงตาม label code)
-- Idempotent: seed เฉพาะแถวที่ยังเป็น null
-- =============================================

ALTER TABLE tbm_gem
    ADD COLUMN IF NOT EXISTS label_code CHARACTER VARYING NULL,
    ADD COLUMN IF NOT EXISTS label_sort INT NULL;

-- Seed: match by base code (ตัดวงเล็บ/ช่องว่างท้ายออก เช่น 'RU(BUR)' -> 'RU', 'DIA (กุหลาบ)' -> 'DIA')
WITH map(base_code, new_label, new_sort) AS (
    VALUES
        ('DI',  'DI', 1),
        ('DIA', 'DI', 1),
        ('RU',  'RU', 2),
        ('SA',  'SA', 3),
        ('EM',  'EM', 4),
        ('PS',  'PS', 5),
        ('YS',  'YS', 6),
        ('AM',  'AM', 7),
        ('BT',  'BT', 8),
        ('CI',  'CI', 9),
        ('GA',  'GA', 10),
        ('PE',  'PE', 11),
        ('PE1', 'PE', 11)
)
UPDATE tbm_gem g
SET label_code = m.new_label,
    label_sort = m.new_sort
FROM map m
WHERE upper(btrim(split_part(g.code, '(', 1))) = m.base_code
  AND g.label_code IS NULL
  AND g.label_sort IS NULL;

-- Verification
SELECT id, code, is_active, label_code, label_sort
FROM tbm_gem
ORDER BY label_sort NULLS LAST, label_code NULLS LAST, code;
