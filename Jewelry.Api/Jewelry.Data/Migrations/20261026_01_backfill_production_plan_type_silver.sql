-- =============================================
-- Migration: TK202610260001 - Backfill tbt_production_plan.type/type_size = 'Silver' for old silver plans
-- Date: 2026-10-26
-- Ticket: TK202610260001
-- Description (TH):
--   สาเหตุ: การรับสินค้าเข้าคลังจากการผลิต (ReceiptProductionService: isSilverPlan = plan.Type == "Silver"
--   และฝั่ง UI data.type === 'Silver') ใช้ tbt_production_plan.type ตัดสินว่าเป็นแผนเงิน (silver lot) หรือไม่
--   แผนเงินเก่าที่สร้างก่อน 2026-09-08 มี type = NULL (และ type_size = NULL) จึงถูกมองเป็นแผนทอง
--   ทำให้ทุกบรรทัดรับกลายเป็น 1 piece และจำนวนที่กรอก (qty) ถูกทิ้ง
--   สคริปต์นี้ตั้ง type = 'Silver' และ type_size = 'Silver' (เฉพาะที่ยัง NULL) ให้แผนที่ type เป็น NULL
--   ที่วัสดุโลหะ (tbt_production_plan_material.gold) เป็นเงินล้วน ('SV'/'Silver')
--   ค่าตรงกับแผนเงินใหม่: type = 'Silver', type_size = 'Silver'
--   แผนที่มีโลหะอื่นปน (ไม่ใช่เงินล้วน) จะไม่ถูกแตะ
-- Description (EN):
--   Root cause: goods receipt from production decides silver-lot behaviour from tbt_production_plan.type.
--   Old silver plans (created before 2026-09-08) have type = NULL so they are treated as gold plans
--   (1 piece per receipt line, entered qty dropped). This sets type/type_size = 'Silver' for active
--   type-NULL plans whose metal materials are all silver. Mixed-metal plans are left untouched.
-- Run: DBeaver บน prod - รัน STEP 1 (preview) ก่อน แล้วค่อยรัน STEP 2, STEP 3
-- Rollback: ดู STEP 4 (commented out)
-- =============================================

-- =============================================
-- STEP 1: PRE-CHECK (read-only)
-- =============================================

-- 1.1 แผนที่จะถูกแก้ (คาดประมาณ 1,033 - ตัวเลขอาจเปลี่ยนตามข้อมูลจริง)
SELECT COUNT(*) AS target_plans
FROM tbt_production_plan p
WHERE p.type IS NULL
  AND p.is_active
  AND EXISTS (SELECT 1 FROM tbt_production_plan_material m
              WHERE m.production_plan_id = p.id AND upper(m.gold) IN ('SV', 'SILVER'))
  AND NOT EXISTS (SELECT 1 FROM tbt_production_plan_material m
                  WHERE m.production_plan_id = p.id AND coalesce(upper(m.gold), '') NOT IN ('SV', 'SILVER', ''));

-- 1.2 แผนที่มีโลหะอื่นปนกับเงิน (จะไม่ถูกแตะ - คาดประมาณ 1)
SELECT p.id, p.wo, p.wo_number, p.type, p.type_size
FROM tbt_production_plan p
WHERE p.type IS NULL
  AND p.is_active
  AND EXISTS (SELECT 1 FROM tbt_production_plan_material m
              WHERE m.production_plan_id = p.id AND upper(m.gold) IN ('SV', 'SILVER'))
  AND EXISTS (SELECT 1 FROM tbt_production_plan_material m
              WHERE m.production_plan_id = p.id AND coalesce(upper(m.gold), '') NOT IN ('SV', 'SILVER', ''))
ORDER BY p.wo, p.wo_number;

-- =============================================
-- STEP 2: UPDATE
-- =============================================
BEGIN;

UPDATE tbt_production_plan p
SET type        = 'Silver',
    type_size   = COALESCE(p.type_size, 'Silver'),
    update_date = now(),
    update_by   = 'TK202610260001'
WHERE p.type IS NULL
  AND p.is_active
  AND EXISTS (SELECT 1 FROM tbt_production_plan_material m
              WHERE m.production_plan_id = p.id AND upper(m.gold) IN ('SV', 'SILVER'))
  AND NOT EXISTS (SELECT 1 FROM tbt_production_plan_material m
                  WHERE m.production_plan_id = p.id AND coalesce(upper(m.gold), '') NOT IN ('SV', 'SILVER', ''));

COMMIT;

-- =============================================
-- STEP 3: POST-CHECK (read-only)
-- =============================================

-- 3.1 แผน type NULL ที่ยังมีวัสดุเงินเหลือ (คาด: เหลือเฉพาะแผนที่มีโลหะปน)
SELECT COUNT(*) AS remaining_null_type_with_silver
FROM tbt_production_plan p
WHERE p.type IS NULL
  AND p.is_active
  AND EXISTS (SELECT 1 FROM tbt_production_plan_material m
              WHERE m.production_plan_id = p.id AND upper(m.gold) IN ('SV', 'SILVER'));

-- 3.2 ตัวอย่างแผนที่รู้จัก (ควรเป็น Silver / Silver)
SELECT p.id, p.wo, p.wo_number, p.type, p.type_size, p.update_date, p.update_by
FROM tbt_production_plan p
WHERE (p.wo, p.wo_number) IN (('6906007', 18), ('6811009', 5), ('6907018', 15))
ORDER BY p.wo, p.wo_number;

-- =============================================
-- STEP 4: ROLLBACK (commented out - รันเมื่อจำเป็นเท่านั้น)
-- หมายเหตุ: จะย้อนทุกแถวที่มี update_by = 'TK202610260001' และ type = 'Silver'
--           รวมถึงที่มีคนแก้ด้วยมือภายหลังแต่ยังคง marker นี้ไว้เท่านั้น
-- =============================================
-- BEGIN;
-- UPDATE tbt_production_plan
-- SET type = NULL,
--     type_size = NULL
-- WHERE update_by = 'TK202610260001'
--   AND type = 'Silver';
-- COMMIT;
